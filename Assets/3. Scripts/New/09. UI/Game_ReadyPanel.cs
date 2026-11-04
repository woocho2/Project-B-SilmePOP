using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 게임 시작 전 뽑기 화면 (GameReadyPanel에 붙임).
//   1) 슬라임 위 숫자(Score)가 10 ~ 15 사이로 빠르게 바뀌다가 점점 느려져 목표 합에 멈춤
//   2) 빨간 네모가 아이템 6개 위를 옮겨 다니다가 점점 느려져 하나에 멈춤 → 3번 반복해서 아이템 3개를 정함
// 결과(목표 합, 아이템 3개)는 경기 seed로 정해져 있어서 모든 플레이어가 같음. 돌아가는 모습만 PC마다 다름.
// 모든 참가자가 게임 씬에 오면 정해지는 playAt(같은 서버 시각)부터 시작하고, 길이도 같아서 모두 같은 순간에 끝남.
// 켜고 끄는 것은 UIManager_Game. 이 패널은 Begin으로 시작해서 끝나면 onFinished를 부름.
public class Game_ReadyPanel : MonoBehaviour
{
    private const string ItemPrefix = "Img_Item_";   // Img_Item_Destroy처럼 뒤에 스킬 이름

    [Header("목표 점수")]
    [Tooltip("슬라임 위 숫자. 비우면 이름이 Score인 글자를 찾음")]
    [SerializeField] private TMP_Text txt_score;
    [SerializeField, Min(0.5f)] private float scoreRollDuration = 5f;

    [Header("아이템")]
    [Tooltip("Img_Item_스킬이름 이미지 6개가 들어 있는 오브젝트. 비우면 이름이 Item인 자식")]
    [SerializeField] private RectTransform itemRoot;
    [Tooltip("아이템 하나를 뽑는 데 걸리는 시간 (3번 함)")]
    [SerializeField, Min(0.5f)] private float itemRollDuration = 5f;
    [SerializeField] private Color selectorColor = new Color(0.95f, 0.15f, 0.15f, 1f);
    [SerializeField, Min(1f)] private float selectorThickness = 8f;
    [SerializeField] private float selectorPadding = 12f;
    [Tooltip("안 뽑힌 아이템의 색 (다 뽑은 뒤)")]
    [SerializeField] private Color unpickedColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("속도")]
    [SerializeField, Min(0.02f)] private float fastestStep = 0.06f;   // 처음 바뀌는 간격
    [SerializeField, Min(0.05f)] private float slowestStep = 0.5f;    // 멈추기 직전 간격
    [SerializeField, Min(0f)] private float pauseAfterStop = 0.6f;    // 멈춘 뒤 잠깐 보여 줌

    [Header("목표 점수 슬라임 날아가기")]
    [Tooltip("숫자를 품은 슬라임. 카운트다운 때 공책에서 튀어나와 작아지며 시간 글자 위로 날아감. 비우면 Score의 부모")]
    [SerializeField] private RectTransform targetSlime;
    [SerializeField, Min(1f)] private float flyPopScale = 1.15f;     // 튀어나올 때 잠깐 커지는 정도
    [SerializeField, Min(0f)] private float flyPopDuration = 0.25f;
    [SerializeField, Min(0.1f)] private float flyDuration = 1.2f;
    [Tooltip("도착했을 때 슬라임의 가로 크기 (px)")]
    [SerializeField, Min(10f)] private float flySize = 90f;
    [Tooltip("시간 글자와 슬라임 사이 틈 (px)")]
    [SerializeField] private float flyGap = 5f;

    [Header("기다리는 중 (선택 사항)")]
    [Tooltip("모두 모일 때까지 '다른 플레이어를 기다리는 중...'을 보여 줄 글자. 비우면 이름 Txt_Waiting")]
    [SerializeField] private TMP_Text txt_waiting;

    private readonly Dictionary<SkillType, Image> items = new Dictionary<SkillType, Image>();
    private readonly List<GameObject> pickedFrames = new List<GameObject>();
    private RectTransform selector;
    private readonly System.Random look = new System.Random();   // 돌아가는 모습만 (결과는 seed)
    private Sequence rollTween;
    private Action finished;
    private bool waiting;
    private bool targetShown;   // 목표 점수가 정해져 슬라임에 보이는 중 (개발용 F1로 바뀌면 따라 바꿈)

    // 슬라임의 원래 자리 (새 판에 공책으로 돌려놓기 위해)
    private Transform slimeHomeParent;
    private Vector2 slimeHomePosition;
    private Vector3 slimeHomeScale;
    private int slimeHomeSibling;

    private void Awake()
    {
        if (txt_score == null) txt_score = FindChild<TMP_Text>(transform, "Score");
        if (itemRoot == null) itemRoot = FindChild<RectTransform>(transform, "Item");
        if (txt_waiting == null) txt_waiting = FindChild<TMP_Text>(transform, "Txt_Waiting");
        if (targetSlime == null && txt_score != null) targetSlime = txt_score.rectTransform.parent as RectTransform;
        if (targetSlime != null)
        {
            slimeHomeParent = targetSlime.parent;
            slimeHomePosition = targetSlime.anchoredPosition;
            slimeHomeScale = targetSlime.localScale;
            slimeHomeSibling = targetSlime.GetSiblingIndex();
        }

        if (itemRoot != null)
        {
            foreach (Image image in itemRoot.GetComponentsInChildren<Image>(true))
            {
                if (!image.name.StartsWith(ItemPrefix)) continue;
                if (Enum.TryParse(image.name.Substring(ItemPrefix.Length), out SkillType type)) items[type] = image;
            }
        }
        if (items.Count < GameRules.SkillsPerMatch)
        {
            Debug.LogWarning($"Game_ReadyPanel: 아이템 이미지({ItemPrefix}스킬이름)를 {items.Count}개만 찾았습니다.", this);
        }
    }

    // 새 판: 처음 모습으로 돌리고, 모두 모이면(playAt) 뽑기를 시작. 끝나면 onFinished.
    public void Begin(Action onFinished)
    {
        Stop();
        Prepare();
        finished = onFinished;

        GameManager game = GameManager.Instance;
        if (game != null && game.IsReadyToCountdown)
        {
            StartRolling();
            return;
        }

        // 방에서 시작한 판: 모든 참가자가 게임 씬에 올 때까지 기다림 (playAt이 정해지면 방 정보가 바뀜)
        waiting = true;
        SetWaiting(true);
        if (NetworkManager.Instance != null) NetworkManager.Instance.RoomChanged += OnRoomChangedWhileWaiting;
    }

    public void Stop()
    {
        StopWaiting();
        rollTween?.Kill();
        rollTween = null;
        finished = null;
    }

    private void OnRoomChangedWhileWaiting(RoomInfo room)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsReadyToCountdown) return;
        StopWaiting();
        StartRolling();
    }

    private void StopWaiting()
    {
        if (!waiting) return;
        waiting = false;
        SetWaiting(false);
        if (NetworkManager.Instance != null) NetworkManager.Instance.RoomChanged -= OnRoomChangedWhileWaiting;
    }

    private void SetWaiting(bool isWaiting)
    {
        if (txt_waiting == null) return;
        txt_waiting.gameObject.SetActive(isWaiting);
        if (isWaiting) txt_waiting.text = "다른 플레이어를 기다리는 중...";
    }

    // playAt까지 기다린 뒤 뽑기. 모두 같은 서버 시각에 시작함.
    private void StartRolling()
    {
        GameManager game = GameManager.Instance;
        rollTween = DOTween.Sequence().SetUpdate(true).SetTarget(this)
            .AppendInterval(game.SecondsUntilStart)
            .Append(Build(game.Settings))
            .OnComplete(() =>
            {
                rollTween = null;
                Action done = finished;
                finished = null;
                done?.Invoke();
            });
    }

    // 뽑기가 끝난 뒤: 슬라임이 공책에서 튀어나와 작아지며 above(시간 글자) 바로 위로 날아감.
    // 이 패널은 곧 닫히므로 above와 같은 부모(게임 화면)로 옮겨 붙인 뒤 날림. 게임 내내 그 자리에 남음.
    public void SendTargetSlime(RectTransform above)
    {
        if (targetSlime == null || above == null) return;

        targetSlime.DOKill();
        targetSlime.SetParent(above.parent, true);   // 보이는 자리는 그대로
        targetSlime.SetAsLastSibling();

        // 도착 자리: above의 윗변 가운데에서 틈 + 슬라임 반만큼 위 (월드 좌표로 계산해서 앵커가 달라도 맞음)
        Rect box = above.rect;
        float lift = (flyGap + flySize * 0.5f) / Mathf.Max(0.0001f, above.localScale.y);
        Vector3 end = above.TransformPoint(new Vector3(box.center.x, box.yMax + lift, 0f));
        float endScale = flySize / Mathf.Max(1f, targetSlime.rect.width);

        Vector3 startScale = targetSlime.localScale;
        DOTween.Sequence().SetUpdate(true).SetTarget(targetSlime)
            .Append(targetSlime.DOScale(startScale * flyPopScale, flyPopDuration).SetEase(Ease.OutBack))
            // 옆으로는 부드럽게, 위로는 먼저 빠르게 올라가서 곡선을 그리며 날아감
            .Append(targetSlime.DOMoveX(end.x, flyDuration).SetEase(Ease.InOutSine))
            .Join(targetSlime.DOMoveY(end.y, flyDuration).SetEase(Ease.OutQuad))
            .Join(targetSlime.DOScale(Vector3.one * endScale, flyDuration).SetEase(Ease.InOutSine));
    }

    // 날아갔던 슬라임을 공책의 원래 자리로
    private void ReturnTargetSlime()
    {
        if (targetSlime == null || slimeHomeParent == null) return;
        targetSlime.DOKill();
        targetSlime.SetParent(slimeHomeParent, false);
        targetSlime.SetSiblingIndex(slimeHomeSibling);
        targetSlime.anchoredPosition = slimeHomePosition;
        targetSlime.localScale = slimeHomeScale;
    }

    // 처음 모습으로
    private void Prepare()
    {
        ReturnTargetSlime();
        targetShown = false;
        if (txt_score != null) txt_score.text = "?";

        foreach (GameObject frame in pickedFrames)
        {
            if (frame != null) Destroy(frame);
        }
        pickedFrames.Clear();
        foreach (Image image in items.Values) image.color = Color.white;
        if (selector != null) selector.gameObject.SetActive(false);
    }

    // 뽑기 연출 전체 (StartRolling의 Sequence 안에 들어가므로 시간 설정은 바깥 것을 따름)
    private Sequence Build(MatchSettings settings)
    {
        Sequence sequence = DOTween.Sequence();

        // 1) 목표 점수
        int last = -1;
        Steps score = StepTimes(scoreRollDuration);
        for (int i = 0; i < score.Times.Count; i++)
        {
            bool isFinal = i == score.Times.Count - 1;
            sequence.InsertCallback(score.Times[i], () =>
            {
                last = isFinal ? settings.TargetSum : OtherNumber(last);
                ShowScore(last, isFinal);
            });
        }
        float at = score.Total + pauseAfterStop;

        // 2) 아이템 3개
        List<SkillType> picked = new List<SkillType>();
        for (int pick = 0; pick < settings.Skills.Count && pick < GameRules.SkillsPerMatch; pick++)
        {
            SkillType result = settings.Skills[pick];
            int slot = pick;
            Steps steps = StepTimes(itemRollDuration);
            SkillType current = SkillType.None;
            for (int i = 0; i < steps.Times.Count; i++)
            {
                bool isFinal = i == steps.Times.Count - 1;
                sequence.InsertCallback(at + steps.Times[i], () =>
                {
                    current = isFinal ? result : OtherItem(current, picked, result);
                    MoveSelector(current, isFinal);
                    if (isFinal) LockItem(result, slot, picked);
                });
            }
            at += steps.Total + pauseAfterStop;
        }

        // 다 뽑으면 안 뽑힌 아이템을 흐리게 하고, 잠깐 보여 준 뒤 끝 (닫는 것은 UIManager_Game)
        sequence.InsertCallback(at - pauseAfterStop, DimUnpicked);
        sequence.InsertCallback(at, () => { });
        return sequence;
    }

    private void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.TargetSumChanged += OnTargetSumChanged;
    }

    private void OnTargetSumChanged(int targetSum)
    {
        if (targetShown && txt_score != null) txt_score.text = targetSum.ToString();
    }

    private void OnDestroy()
    {
        Stop();
        if (targetSlime != null) targetSlime.DOKill();
        if (GameManager.Instance != null) GameManager.Instance.TargetSumChanged -= OnTargetSumChanged;
    }

    // ── 숫자 ────────────────────────────────────────────────────

    private void ShowScore(int number, bool isFinal)
    {
        if (txt_score == null) return;
        txt_score.text = number.ToString();
        txt_score.rectTransform.DOKill(true);
        if (isFinal)
        {
            targetShown = true;
            SoundManager.Play(SoundEffect.Hint);
            txt_score.rectTransform.DOPunchScale(Vector3.one * 0.4f, 0.45f, 6, 0.6f).SetUpdate(true);
        }
        else
        {
            SoundManager.Play(SoundEffect.Click);
        }
    }

    // 방금과 다른 10 ~ 15
    private int OtherNumber(int previous)
    {
        int count = GameRules.MaxTargetSum - GameRules.MinTargetSum + 1;
        int next;
        do { next = GameRules.MinTargetSum + look.Next(count); } while (next == previous && count > 1);
        return next;
    }

    // ── 아이템 ──────────────────────────────────────────────────

    // 아직 안 뽑힌 것 중 방금과 다른 하나. 마지막에 result로 멈춤.
    private SkillType OtherItem(SkillType previous, List<SkillType> picked, SkillType result)
    {
        List<SkillType> candidates = new List<SkillType>();
        foreach (SkillType type in items.Keys)
        {
            if (!picked.Contains(type) && type != previous) candidates.Add(type);
        }
        if (candidates.Count == 0) return result;
        return candidates[look.Next(candidates.Count)];
    }

    private void MoveSelector(SkillType type, bool isFinal)
    {
        if (!items.TryGetValue(type, out Image item)) return;

        RectTransform frame = Selector();
        frame.gameObject.SetActive(true);
        Place(frame, item.rectTransform);
        frame.SetAsLastSibling();
        frame.DOKill(true);
        if (isFinal) frame.DOPunchScale(Vector3.one * 0.2f, 0.35f, 6, 0.6f).SetUpdate(true);
        SoundManager.Play(isFinal ? SoundEffect.Hint : SoundEffect.Click);
    }

    // 뽑힌 아이템에 빨간 네모를 남기고, 게임 화면의 slot번째 칸을 채움
    private void LockItem(SkillType type, int slot, List<SkillType> picked)
    {
        picked.Add(type);
        if (!items.TryGetValue(type, out Image item)) return;

        GameObject stay = Instantiate(Selector().gameObject, itemRoot);
        stay.name = $"Picked_{type}";
        Place((RectTransform)stay.transform, item.rectTransform);
        pickedFrames.Add(stay);
        Selector().gameObject.SetActive(false);

        if (SkillManager.Instance != null) SkillManager.Instance.RevealSlot(slot, item.sprite);
    }

    private void DimUnpicked()
    {
        List<SkillType> kept = new List<SkillType>();
        foreach (GameObject frame in pickedFrames)
        {
            if (frame != null && Enum.TryParse(frame.name.Substring("Picked_".Length), out SkillType type)) kept.Add(type);
        }
        foreach (KeyValuePair<SkillType, Image> pair in items)
        {
            if (!kept.Contains(pair.Key)) pair.Value.DOColor(unpickedColor, 0.3f).SetUpdate(true);
        }
    }

    // 아이템과 같은 자리·크기(+여백)에 네모를 둠 (아이템과 같은 부모 아래)
    private void Place(RectTransform frame, RectTransform item)
    {
        frame.anchorMin = item.anchorMin;
        frame.anchorMax = item.anchorMax;
        frame.pivot = item.pivot;
        frame.anchoredPosition = item.anchoredPosition;
        frame.sizeDelta = item.sizeDelta + Vector2.one * (selectorPadding * 2f);
        frame.localScale = Vector3.one;
    }

    // 빨간 네모: 테두리 4개를 붙인 빈 오브젝트 (그림 파일 없이 만듦)
    private RectTransform Selector()
    {
        if (selector != null) return selector;

        selector = new GameObject("Selector", typeof(RectTransform)).GetComponent<RectTransform>();
        selector.SetParent(itemRoot, false);
        selector.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        AddEdge("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, selectorThickness));
        AddEdge("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, selectorThickness));
        AddEdge("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(selectorThickness, 0f));
        AddEdge("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(selectorThickness, 0f));
        selector.gameObject.SetActive(false);
        return selector;
    }

    private void AddEdge(string edgeName, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        RectTransform edge = new GameObject(edgeName, typeof(RectTransform)).GetComponent<RectTransform>();
        edge.SetParent(selector, false);
        edge.anchorMin = anchorMin;
        edge.anchorMax = anchorMax;
        edge.pivot = new Vector2(Mathf.Approximately(anchorMin.x, anchorMax.x) ? anchorMin.x : 0.5f,
                                 Mathf.Approximately(anchorMin.y, anchorMax.y) ? anchorMin.y : 0.5f);
        edge.sizeDelta = size;
        edge.anchoredPosition = Vector2.zero;
        Image image = edge.gameObject.AddComponent<Image>();
        image.color = selectorColor;
        image.raycastTarget = false;
    }

    // ── 점점 느려지는 간격 ──────────────────────────────────────

    private struct Steps
    {
        public List<float> Times;   // 바뀌는 시각 (마지막이 멈추는 시각)
        public float Total;
    }

    // duration 동안 간격이 fastestStep에서 slowestStep으로 점점 길어짐 (뒤로 갈수록 확 느려지게 제곱)
    private Steps StepTimes(float duration)
    {
        List<float> times = new List<float>();
        float t = 0f;
        while (t < duration)
        {
            times.Add(t);
            float progress = Mathf.Clamp01(t / duration);
            t += Mathf.Lerp(fastestStep, slowestStep, progress * progress);
        }
        times.Add(t);   // 멈춤
        return new Steps { Times = times, Total = t };
    }

    private static T FindChild<T>(Transform root, string childName) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.name == childName) return component;
        }
        return null;
    }
}
