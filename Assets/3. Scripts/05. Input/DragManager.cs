using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragManager : Singleton<DragManager>
{
    [Header("World Drag Box Prefab/Object")]
    [SerializeField] private GameObject dragBoxPrefab;
    private GameObject currentDragBox;

    private Vector2 startPosWorld;
    private bool isDragging = false;

    // 내가 끌어서 터뜨림 / 합이 안 맞음 (개발용 기록 모드가 행동 패턴을 남김). 아이템으로 없앤 것은 들어가지 않음.
    public static event System.Action<IReadOnlyList<BoardSlime>> Popped;
    public static event System.Action<IReadOnlyList<BoardSlime>> Missed;

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
            EvaluateDrag(startPosWorld, endPosWorld);

            if (currentDragBox != null)
            {
                currentDragBox.SetActive(false);
            }
        }
    }

    // 놓는 순간의 판 기준으로 판정. 트리거 목록은 강조용이라 빠르게 끌면 놓칠 수 있어 쓰지 않음.
    private void EvaluateDrag(Vector2 start, Vector2 end)
    {
        Vector2 center = (start + end) / 2f;
        Vector2 size = new Vector2(Mathf.Abs(start.x - end.x), Mathf.Abs(start.y - end.y));

        List<int> cells = new List<int>();
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            SlimeController view = hit.GetComponent<SlimeController>();
            if (view != null) cells.Add(view.Cell);
        }

        GameManager game = GameManager.Instance;
        List<BoardSlime> slimes = game.Board.GetSlimesInCells(cells);
        if (slimes.Count == 0) return;

        if (MatchFinder.SumNumbers(slimes) != game.TargetSum)
        {
            SoundManager.Play(SoundEffect.Miss);
            Missed?.Invoke(slimes);
            return;
        }

        int score = ScoreCalculator.Calculate(slimes, game.TargetSum);
        Popped?.Invoke(slimes);
        game.Board.Remove(slimes);
        SoundManager.Play(SoundEffect.Match);
        game.AddScore(score);
        game.CheckForMoves();
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
        if (currentDragBox != null) currentDragBox.SetActive(false);
    }

    private void OnDisable() => CancelDrag();

    protected override void OnDestroy()
    {
        if (currentDragBox != null) Destroy(currentDragBox);
        base.OnDestroy();
    }
}
