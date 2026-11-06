using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragManager : Singleton<DragManager>
{
    [Header("World Drag Box Prefab/Object")]
    [SerializeField] private GameObject dragBoxPrefab;
    private GameObject currentDragBox;

    private Vector2 startPosWorld;
    private bool isDragging = false;
    private Sequence aiDrag;   // 자동 플레이(AiAutoPlay)에서 AI가 끄는 중
    private Tween missShake;   // 합이 틀렸을 때 상자가 흔들리는 중

    private void Start()
    {
        if (dragBoxPrefab != null)
        {
            currentDragBox = Instantiate(dragBoxPrefab);
            currentDragBox.SetActive(false);
        }
        else
        {
            Debug.LogError("DragManager에 dragBoxPrefab이 할당되지 않았습니다!");
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            CancelDrag();
            return;
        }
        if (AiAutoPlay.IsOn) return;   // AI가 대신 둠. 내 마우스는 쓰지 않음
        if (Mouse.current == null) return;

        // 스킬 대상을 고르는 중이거나, 이번 클릭을 스킬 적용에 썼으면 드래그하지 않음
        if (SkillManager.Instance != null && (SkillManager.Instance.IsTargeting || SkillManager.Instance.ConsumedClickThisFrame))
        {
            CancelDrag();
            return;
        }

        // 1. 드래그 시작
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            KillMissShake();
            isDragging = true;

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            startPosWorld = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            if (currentDragBox != null)
            {
                currentDragBox.SetActive(true);
                UpdateDragBoxTransform(startPosWorld, startPosWorld);
            }
        }

        // 2. 드래그 진행 중
        if (Mouse.current.leftButton.isPressed && isDragging)
        {
            if (currentDragBox != null)
            {
                Vector2 currentPosWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                UpdateDragBoxTransform(startPosWorld, currentPosWorld);
            }
        }

        // 3. 드래그 종료 및 조건 판정
        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;

            Vector2 endPosWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            HideBox(EvaluateDrag(startPosWorld, endPosWorld));
        }
    }

    // 놓은 뒤 상자 치우기. 합이 틀렸으면 빨간 강조가 남은 채로 좌우로 흔들린 뒤 사라짐.
    private void HideBox(bool missed)
    {
        if (currentDragBox == null) return;
        if (!missed)
        {
            currentDragBox.SetActive(false);
            return;
        }

        KillMissShake();
        Transform box = currentDragBox.transform;
        Vector3 home = box.position;
        missShake = box.DOShakePosition(0.28f, new Vector3(0.12f, 0f, 0f), 30, 0f, false, true)
            .OnComplete(() =>
            {
                missShake = null;
                box.position = home;
                currentDragBox.SetActive(false);
            })
            .SetLink(gameObject);
    }

    private void KillMissShake()
    {
        if (missShake == null) return;
        missShake.Kill();
        missShake = null;
    }

    // 놓는 순간의 판 기준으로 판정. 트리거 목록은 강조용이라 빠르게 끌면 놓칠 수 있어 쓰지 않음. 합이 틀렸으면 true.
    private bool EvaluateDrag(Vector2 start, Vector2 end)
    {
        Vector2 center = (start + end) / 2f;
        Vector2 size = new Vector2(Mathf.Abs(start.x - end.x), Mathf.Abs(start.y - end.y));

        List<int> cells = new List<int>();
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            SlimeController view = hit.GetComponent<SlimeController>();
            if (view != null) cells.Add(view.Cell);
        }
        return EvaluateCells(cells);
    }

    // 이 칸들을 끌어서 놓은 것과 같음: 합이 맞으면 터뜨리고 점수(+점수 글자), 아니면 실패 소리. 합이 틀렸으면 true.
    private bool EvaluateCells(List<int> cells)
    {
        GameManager game = GameManager.Instance;
        List<BoardSlime> slimes = game.Board.GetSlimesInCells(cells);
        if (slimes.Count == 0) return false;

        if (MatchFinder.SumNumbers(slimes) != game.TargetSum)
        {
            SoundManager.Play(SoundEffect.Miss);
            return true;
        }

        int score = ScoreCalculator.Calculate(slimes, game.TargetSum);
        game.Board.Remove(slimes);
        SoundManager.Play(SoundEffect.Match);
        if (SlimeManager.Instance != null) SlimeManager.Instance.ShowScore(AnchorCells(slimes), score);
        game.AddScore(score);
        game.CheckForMoves();
        return false;
    }

    private static IEnumerable<int> AnchorCells(List<BoardSlime> slimes)
    {
        foreach (BoardSlime slime in slimes) yield return slime.Anchor;
    }

    // 자동 플레이: AI가 사람처럼 사각형을 끔. start에서 상자를 눌러 end까지 늘리고, 잠깐 멈춘 뒤 놓음.
    // 놓는 순간 rect 칸들로 판정 (그사이 조커가 움직여 합이 틀리면 사람처럼 실패). 끝나면 onDone.
    public void PlayAiDrag(Vector2 start, Vector2 end, BoardRect rect, float duration, System.Action onDone)
    {
        CancelDrag();
        if (currentDragBox == null)
        {
            EvaluateCells(rect.GetCells());
            onDone?.Invoke();
            return;
        }


        currentDragBox.SetActive(true);
        UpdateDragBoxTransform(start, start);

        aiDrag = DOTween.Sequence()
            .Append(DOVirtual.Float(0f, 1f, duration, t => UpdateDragBoxTransform(start, Vector2.LerpUnclamped(start, end, t)))
                             .SetEase(Ease.OutCubic))
            .AppendInterval(0.12f)
            .OnComplete(() =>
            {
                aiDrag = null;
                HideBox(EvaluateCells(rect.GetCells()));
                onDone?.Invoke();
            })
            .SetLink(gameObject);
    }

    public void ResetDragManager()
    {
        CancelDrag();

        if (currentDragBox != null)
        {
            currentDragBox.SetActive(false);
        }
    }

    private void UpdateDragBoxTransform(Vector2 start, Vector2 end)
    {
        Vector2 center = (start + end) / 2f;
        currentDragBox.transform.position = center;

        float width = Mathf.Abs(start.x - end.x);
        float height = Mathf.Abs(start.y - end.y);

        currentDragBox.transform.localScale = new Vector3(width, height, 1f);
    }

    private void CancelDrag()
    {
        isDragging = false;
        KillMissShake();
        if (aiDrag != null)
        {
            aiDrag.Kill();   // 놓지 않고 그만둠 (판정 없음)
            aiDrag = null;
        }
        if (currentDragBox != null) currentDragBox.SetActive(false);
    }

    private void OnDisable() => CancelDrag();

    protected override void OnDestroy()
    {
        aiDrag?.Kill();
        if (currentDragBox != null) Destroy(currentDragBox);
        base.OnDestroy();
    }
}
