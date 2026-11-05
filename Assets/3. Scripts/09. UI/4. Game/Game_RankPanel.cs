using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

// 게임 화면의 실시간 순위 (RankPanel에 붙임).
// 참가자마다 슬라임 하나(씬의 Slime을 복사)를 두고, 그 플레이어의 색으로 Base를 칠하고 아래에 이름과 점수를 적음.
// 점수가 바뀌어 순위가 바뀌면 슬라임이 그 등수 글자(Txt_Rank1 ~ 4) 아래로 통통 튀며 천천히 옮겨 감.
// 이름·점수 글자는 그라데이션과 숨쉬는 빛으로 반짝이고, 내 줄은 금빛으로 더 밝음. 점수가 오르면 번쩍임.
// 점수는 서버(NetworkManager.Scores)에서 오고, 내 점수는 서버를 기다리지 않고 바로 반영함.
// 순위 규칙은 MatchScores와 같음 (점수 높은 순 → 동점이면 먼저 도달한 순 → 기권은 맨 아래).
public class Game_RankPanel : MonoBehaviour
{
    [Tooltip("등수 글자 4개 (1등 ~ 4등 순서). 비우면 이름 Txt_Rank1 ~ 4로 찾음")]
    [SerializeField] private RectTransform[] rankLabels = new RectTransform[GameRules.MaxPlayers];
    [Tooltip("본보기 슬라임 (1등 글자 아래에 둔 것). 등수 글자와의 거리를 그대로 모든 등수에 씀. 비우면 이름 Slime인 자식")]
    [SerializeField] private RectTransform slimeTemplate;
    [Header("이름·점수 글자 (굵게)")]
    [Tooltip("본보기 슬라임 안에 Txt_Name, Txt_Score 글자를 만들어 두면 그것을 씀. 없으면 이름은 슬라임 바로 밑, 점수는 슬라임 가운데에 만듦")]
    [SerializeField] private float nameGap = 2f;   // 슬라임 몸통 아래 끝과 이름 사이
    [SerializeField] private float nameFontSize = 24f;
    [SerializeField] private float scoreFontSize = 28f;
    [SerializeField] private Color myNameColor = new Color(0.2f, 0.45f, 0.9f);
    [Tooltip("상대방의 이름·점수 색")]
    [SerializeField] private Color otherTextColor = Color.black;

    [Header("이동")]
    [SerializeField, Min(0.1f)] private float moveDuration = 0.8f;
    [SerializeField] private float jumpPower = 40f;

    [Header("반짝임")]
    [Tooltip("글자 둘레의 빛. 내 줄은 더 진하고 크게 숨쉬듯 빛남")]
    [SerializeField] private Color glowColor = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private Color myGlowColor = new Color(1f, 0.85f, 0.3f, 0.9f);
    [Tooltip("점수가 오를 때 번쩍이는 빛")]
    [SerializeField] private Color flashColor = new Color(1f, 1f, 0.7f, 1f);
    [SerializeField, Min(0.1f)] private float glowPulseDuration = 0.8f;

    private const float GlowSoftness = 0.6f;
    private const float GlowDilateMin = 0.1f;
    private const float GlowDilateMax = 0.45f;

    private sealed class Row
    {
        public RectTransform root;
        public SlimeTint tint;
        public TMP_Text name;
        public TMP_Text score;
        public Material nameGlow;
        public Material scoreGlow;
        public Sequence pulse;
        public bool isMe;
        public int rank = -1;
        public int shownScore = -1;
    }

    private readonly Dictionary<int, Row> rows = new Dictionary<int, Row>();   // 자리 → 줄
    private Vector2 offsetFromLabel;
    private TMP_Text textStyle;

    private void Awake()
    {
        for (int i = 0; i < rankLabels.Length; i++)
        {
            if (rankLabels[i] == null) rankLabels[i] = FindChild<RectTransform>($"Txt_Rank{i + 1}");
        }
        // 프리팹 원본이 연결돼 있으면 씬의 슬라임을 씀 (원본을 복사하면 캔버스 밖에 생겨 안 보이고, 원본이 꺼져 버림)
        if (slimeTemplate != null && !slimeTemplate.gameObject.scene.IsValid()) slimeTemplate = null;
        if (slimeTemplate == null) slimeTemplate = FindChild<RectTransform>("Slime");
        if (rankLabels[0] != null) textStyle = rankLabels[0].GetComponent<TMP_Text>();

        // 본보기가 1등 글자 안에 들어 있으면 글자와 같은 부모로 꺼냄 (등수 글자와의 거리를 같은 좌표로 재기 위해)
        if (slimeTemplate != null && rankLabels[0] != null && slimeTemplate.parent == rankLabels[0])
        {
            slimeTemplate.SetParent(rankLabels[0].parent, true);
        }

        if (slimeTemplate == null || rankLabels[0] == null)
        {
            Debug.LogWarning("Game_RankPanel: 본보기 슬라임(Slime)이나 1등 글자(Txt_Rank1)를 찾지 못했습니다.", this);
            return;
        }
        offsetFromLabel = slimeTemplate.anchoredPosition - rankLabels[0].anchoredPosition;
        slimeTemplate.gameObject.SetActive(false);   // 본보기는 숨기고 복사본을 씀
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.MatchCreated += Rebuild;
            GameManager.Instance.LocalScoreChanged += OnLocalScoreChanged;
        }
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.ScoresChanged += OnScoresChanged;
            NetworkManager.Instance.RoomChanged += OnRoomChanged;
        }
        Rebuild();
    }

    private void OnDestroy()
    {
        foreach (Row row in rows.Values) StopTweens(row);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.MatchCreated -= Rebuild;
            GameManager.Instance.LocalScoreChanged -= OnLocalScoreChanged;
        }
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.ScoresChanged -= OnScoresChanged;
            NetworkManager.Instance.RoomChanged -= OnRoomChanged;
        }
    }

    private void OnLocalScoreChanged(int score, float time) => Refresh(false);
    private void OnScoresChanged(MatchScores scores) => Refresh(false);
    private void OnRoomChanged(RoomInfo room) => Refresh(false);

    // 새 판: 줄을 다시 만들고 애니메이션 없이 제자리에
    private void Rebuild()
    {
        foreach (Row row in rows.Values) RemoveRow(row);
        rows.Clear();
        Refresh(true);
    }

    private void Refresh(bool instant)
    {
        if (slimeTemplate == null || rankLabels[0] == null) return;

        List<ScoreEntry> ranking = CurrentRanking();

        // 빠진 자리(나간 사람)는 줄을 지움
        List<int> gone = new List<int>();
        foreach (int slot in rows.Keys)
        {
            if (!ranking.Exists(entry => entry.slot == slot)) gone.Add(slot);
        }
        foreach (int slot in gone)
        {
            RemoveRow(rows[slot]);
            rows.Remove(slot);
        }

        for (int rank = 0; rank < ranking.Count && rank < rankLabels.Length; rank++)
        {
            ScoreEntry entry = ranking[rank];
            if (!rows.TryGetValue(entry.slot, out Row row))
            {
                row = CreateRow(entry.slot);
                rows[entry.slot] = row;
            }
            Draw(row, entry);
            MoveTo(row, rank, instant || row.rank < 0);   // 처음 생긴 줄은 제자리에 바로
        }

        // 사람이 없는 등수 글자는 숨김
        for (int i = 0; i < rankLabels.Length; i++)
        {
            if (rankLabels[i] != null) rankLabels[i].gameObject.SetActive(i < ranking.Count);
        }
    }

    // 서버 점수 + 내 점수는 바로. 점수가 아직 없으면(시작 전) 방의 참가자를 0점으로.
    private List<ScoreEntry> CurrentRanking()
    {
        NetworkManager network = NetworkManager.Instance;
        GameManager game = GameManager.Instance;
        bool isNetworkMatch = network != null && network.CurrentMatch != null && network.CurrentRoom != null;
        int mySlot = isNetworkMatch ? network.MySlot : 0;

        Dictionary<int, ScoreEntry> bySlot = new Dictionary<int, ScoreEntry>();
        if (isNetworkMatch)
        {
            RoomInfo room = network.CurrentRoom;
            for (int slot = 0; slot < room.slots.Length; slot++)
            {
                if (room.slots[slot].kind != SlotKind.Empty) bySlot[slot] = new ScoreEntry { slot = slot };
            }
            foreach (ScoreEntry entry in network.Scores.GetRanking())
            {
                if (bySlot.ContainsKey(entry.slot)) bySlot[entry.slot] = entry.Clone();
            }
        }
        else
        {
            bySlot[0] = new ScoreEntry { slot = 0 };   // 게임 씬을 혼자 시험할 때
        }

        if (game != null && bySlot.TryGetValue(mySlot, out ScoreEntry mine) && game.Score >= mine.score)
        {
            mine.score = game.Score;
            mine.lastScoreTime = game.LastScoreTime;
        }

        MatchScores sorter = new MatchScores();
        sorter.SetEntries(new List<ScoreEntry>(bySlot.Values));
        return sorter.GetRanking();
    }

    private Row CreateRow(int slot)
    {
        RectTransform root = Instantiate(slimeTemplate, slimeTemplate.parent);
        root.name = $"Rank_Slot{slot + 1}";
        root.gameObject.SetActive(true);

        Row row = new Row
        {
            root = root,
            tint = new SlimeTint(root.gameObject),
            name = FindIn<TMP_Text>(root, "Txt_Name") ?? CreateText(root, "Txt_Name", NameOffsetY(root), nameFontSize),
            score = FindIn<TMP_Text>(root, "Txt_Score") ?? CreateText(root, "Txt_Score", 0f, scoreFontSize),   // 슬라임 가운데
            isMe = IsMe(slot),
        };
        ApplyShine(row);
        return row;
    }

    private static bool IsMe(int slot)
    {
        NetworkManager network = NetworkManager.Instance;
        return network == null || network.CurrentRoom == null ? slot == 0 : slot == network.MySlot;
    }

    private void RemoveRow(Row row)
    {
        StopTweens(row);
        if (row.root != null) Destroy(row.root.gameObject);
    }

    private static void StopTweens(Row row)
    {
        row.pulse?.Kill();
        if (row.root != null) row.root.DOKill();
        if (row.nameGlow != null) DOTween.Kill(row.nameGlow);
        if (row.scoreGlow != null) DOTween.Kill(row.scoreGlow);
    }

    private void Draw(Row row, ScoreEntry entry)
    {
        NetworkManager network = NetworkManager.Instance;
        SlotInfo info = network != null && network.CurrentRoom != null && entry.slot < network.CurrentRoom.slots.Length
            ? network.CurrentRoom.slots[entry.slot] : null;

        string displayName = info != null ? TextLines.Nickname(info.displayName)
                           : network != null && network.CurrentUser != null ? network.CurrentUser.nickname : "나";
        row.name.text = displayName;

        string color = info != null ? info.slimeColor : "#" + ColorUtility.ToHtmlStringRGB(StyleSettings.SelectedColor);
        row.tint.Apply(color);

        if (entry.forfeited)
        {
            row.score.text = "기권";
            row.shownScore = -1;
        }
        else if (entry.score != row.shownScore)
        {
            bool increased = row.shownScore >= 0 && entry.score > row.shownScore;
            row.shownScore = entry.score;
            row.score.text = entry.score.ToString();
            if (increased) Flash(row);
        }
    }

    // ── 반짝임 ──────────────────────────────────────────────────

    // 이름·점수는 굵게. 내 글자는 위가 밝은 파란 그라데이션 + 금빛, 상대 글자는 검은색 + 은은한 흰빛.
    // 빛(TMP Underlay)은 숨쉬듯 커졌다 작아짐. 내 줄은 더 진하고 크게 빛남.
    private void ApplyShine(Row row)
    {
        Color glow = row.isMe ? myGlowColor : glowColor;
        foreach (TMP_Text text in new[] { row.name, row.score })
        {
            text.fontStyle |= FontStyles.Bold;
            if (row.isMe) Gradient(text, myNameColor);
            else Solid(text, otherTextColor);
        }
        row.nameGlow = AddGlow(row.name, glow);
        row.scoreGlow = AddGlow(row.score, glow);

        float strength = row.isMe ? 1f : 0.4f;
        float dilateTo = Mathf.Lerp(GlowDilateMin, GlowDilateMax, strength);
        row.pulse = DOTween.Sequence().SetLoops(-1, LoopType.Yoyo)
            .Join(row.nameGlow.DOFloat(dilateTo, "_UnderlayDilate", glowPulseDuration).SetEase(Ease.InOutSine))
            .Join(row.scoreGlow.DOFloat(dilateTo, "_UnderlayDilate", glowPulseDuration).SetEase(Ease.InOutSine));
        // 내 이름은 살짝 커졌다 작아짐 (점수는 오를 때 톡 튀는 것과 겹치지 않게 크기는 그대로)
        if (row.isMe) row.pulse.Join(row.name.rectTransform.DOScale(1.08f, glowPulseDuration).SetEase(Ease.InOutSine));
        // 줄마다 반짝이는 때가 조금씩 다르게
        row.pulse.Goto(Random.value * glowPulseDuration, true);
    }

    // 점수가 오름: 점수 빛이 번쩍했다가 원래 빛으로, 숫자가 톡 튐
    private void Flash(Row row)
    {
        Color glow = row.isMe ? myGlowColor : glowColor;
        if (row.scoreGlow != null)
        {
            DOTween.Kill(row.scoreGlow, "flash");
            row.scoreGlow.SetColor(ShaderUtilities.ID_UnderlayColor, flashColor);
            row.scoreGlow.DOColor(glow, "_UnderlayColor", 0.5f).SetId("flash");
        }
        row.score.rectTransform.DOPunchScale(Vector3.one * 0.35f, 0.35f, 6, 0.6f);
    }

    private static void Gradient(TMP_Text text, Color baseColor)
    {
        Color top = Color.Lerp(baseColor, Color.white, 0.55f);
        text.color = Color.white;   // 그라데이션 색이 그대로 보이게
        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(top, top, baseColor, baseColor);
    }

    private static void Solid(TMP_Text text, Color color)
    {
        text.enableVertexGradient = false;
        text.color = color;
    }

    // 이름 글자의 세로 위치: 슬라임 몸통(Base) 아래 끝 바로 밑. Base가 없으면 슬라임 칸의 아래 끝.
    private float NameOffsetY(RectTransform slime)
    {
        RectTransform body = FindIn<RectTransform>(slime, "Base");
        float bottom = body != null ? body.localPosition.y + body.rect.yMin * body.localScale.y : slime.rect.yMin;
        float textHeight = nameFontSize + 10f;
        return bottom - nameGap - textHeight * 0.5f;
    }

    // 글자마다 머티리얼을 따로 만들어(fontMaterial) 다른 글자에 빛이 번지지 않게 함
    private static Material AddGlow(TMP_Text text, Color glow)
    {
        Material material = text.fontMaterial;
        material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        material.SetColor(ShaderUtilities.ID_UnderlayColor, glow);
        material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
        material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, GlowSoftness);
        material.SetFloat(ShaderUtilities.ID_UnderlayDilate, GlowDilateMax);   // 가장 클 때 기준으로 여백을 잡음
        text.extraPadding = true;
        text.UpdateMeshPadding();
        material.SetFloat(ShaderUtilities.ID_UnderlayDilate, GlowDilateMin);
        return material;
    }

    // rank등 글자 아래 자리로. 순위가 바뀐 것만 통통 튀며 옮겨 감
    private void MoveTo(Row row, int rank, bool instant)
    {
        if (row.rank == rank && !instant) return;
        row.rank = rank;

        Vector2 target = rankLabels[rank].anchoredPosition + offsetFromLabel;
        row.root.DOKill();
        if (instant)
        {
            row.root.anchoredPosition = target;
            return;
        }
        row.root.DOJumpAnchorPos(target, jumpPower, 1, moveDuration).SetEase(Ease.InOutSine);
    }

    // 슬라임 안 offsetY 높이에 가운데 정렬 글자를 만듦. 글꼴은 등수 글자와 같게 (색과 굵기는 ApplyShine).
    private TMP_Text CreateText(RectTransform parent, string textName, float offsetY, float fontSize)
    {
        GameObject go = new GameObject(textName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (textStyle != null)
        {
            text.font = textStyle.font;
            text.color = textStyle.color;
        }
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(190f, fontSize + 10f);
        rect.anchoredPosition = new Vector2(0f, offsetY);
        return text;
    }

    private T FindChild<T>(string childName) where T : Component => FindIn<T>(transform, childName);

    private static T FindIn<T>(Transform root, string childName) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.name == childName) return component;
        }
        return null;
    }
}
