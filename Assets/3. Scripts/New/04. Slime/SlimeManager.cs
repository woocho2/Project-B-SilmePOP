using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

// 내 판의 화면. BoardModel의 이벤트를 듣고 칸을 다시 그리기만 함.
public class SlimeManager : Singleton<SlimeManager>
{
    [Header("Slime Settings")]
    [SerializeField] GameObject slimePrefab;

    [Header("Grid Settings")]
    [SerializeField] Vector2 areaSize = new Vector2(10f, 6f);

    private SlimeController[] cellViews;
    private BoardModel board;

    public bool IsDestroySkillActive { get; private set; } = false;

    private SlimeController currentHoveredSlime;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (IsDestroySkillActive && Mouse.current != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            SlimeController targetSlime = null;
            if (hit.collider != null)
            {
                targetSlime = hit.collider.GetComponent<SlimeController>();
            }

            if (currentHoveredSlime != targetSlime)
            {
                if (currentHoveredSlime != null)
                {
                    currentHoveredSlime.SetDestroyHoverHighlight(false);
                }

                if (targetSlime != null)
                {
                    targetSlime.SetDestroyHoverHighlight(true);
                }

                currentHoveredSlime = targetSlime;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (targetSlime != null)
                {
                    DestroySpecificSlimeAndAddScore(targetSlime);
                }

                EndDestroySkill();
            }
        }
    }

    // 판을 처음부터 다시 그림. 칸 오브젝트는 처음 한 번만 만들고 재대결 때도 다시 씀.
    public void ShowBoard(BoardModel newBoard)
    {
        if (cellViews == null) GenerateCellViews();
        if (cellViews == null) return;

        SubscribeBoard(false);
        board = newBoard;
        SubscribeBoard(true);

        for (int cell = 0; cell < cellViews.Length; cell++)
        {
            RefreshCell(cell);
        }
    }

    private void SubscribeBoard(bool subscribe)
    {
        if (board == null) return;

        if (subscribe)
        {
            board.SlimesRemoved += OnSlimesRemoved;
            board.SlimeChanged += OnSlimeChanged;
            board.SlimeMoved += OnSlimeMoved;
            board.SlimesMerged += OnSlimesMerged;
        }
        else
        {
            board.SlimesRemoved -= OnSlimesRemoved;
            board.SlimeChanged -= OnSlimeChanged;
            board.SlimeMoved -= OnSlimeMoved;
            board.SlimesMerged -= OnSlimesMerged;
        }
    }

    private void OnSlimesRemoved(IReadOnlyList<BoardSlime> slimes)
    {
        RefreshSlimes(slimes);
    }

    private void OnSlimeChanged(BoardSlime slime)
    {
        RefreshCells(slime.Cells);
    }

    private void OnSlimeMoved(BoardSlime slime, int from, int to)
    {
        RefreshCell(from);
        RefreshCell(to);
    }

    private void OnSlimesMerged(BoardSlime merged, IReadOnlyList<BoardSlime> removed)
    {
        RefreshSlimes(removed);
        RefreshCells(merged.Cells);
    }

    private void RefreshSlimes(IReadOnlyList<BoardSlime> slimes)
    {
        foreach (BoardSlime slime in slimes)
        {
            RefreshCells(slime.Cells);
        }
    }

    private void RefreshCells(IReadOnlyList<int> cells)
    {
        foreach (int cell in cells)
        {
            RefreshCell(cell);
        }
    }

    private void RefreshCell(int cell)
    {
        cellViews[cell].Refresh(board.GetSlimeAt(cell));
    }

    public void ActivateDestroySkill()
    {
        IsDestroySkillActive = true;
        ChangeAllSlimesColliderSize(1f, 1f);
        Debug.Log("파괴 스킬 장전 완료: 타겟 슬라임을 클릭하세요.");
    }

    private void EndDestroySkill()
    {
        IsDestroySkillActive = false;
        ChangeAllSlimesColliderSize(0.1f, 0.1f);

        if (currentHoveredSlime != null)
        {
            currentHoveredSlime.SetDestroyHoverHighlight(false);
            currentHoveredSlime = null;
        }
    }

    private void ChangeAllSlimesColliderSize(float sizeX, float sizeY)
    {
        if (cellViews == null) return;

        foreach (SlimeController view in cellViews)
        {
            view.SetColliderSize(sizeX, sizeY);
        }
    }

    private void DestroySpecificSlimeAndAddScore(SlimeController targetSlime)
    {
        BoardSlime slime = board.GetSlimeAt(targetSlime.Cell);
        if (slime == null) return;

        SoundManager.Play(SoundEffect.Destroy);
        board.Remove(new[] { slime });

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(ScoreCalculator.DestroyScore(slime));
            GameManager.Instance.CheckForMoves();
        }
    }

    private void GenerateCellViews()
    {
        if (slimePrefab == null)
        {
            Debug.LogError("슬라임 프리팹이 할당되지 않았습니다.");
            return;
        }

        cellViews = new SlimeController[GameRules.CellCount];

        Vector2 startPos = new Vector2(
            transform.position.x - (areaSize.x / 2f),
            transform.position.y - (areaSize.y / 2f)
        );

        float spacingX = areaSize.x / (GameRules.Columns - 1);
        float spacingY = areaSize.y / (GameRules.Rows - 1);

        for (int y = 0; y < GameRules.Rows; y++)
        {
            for (int x = 0; x < GameRules.Columns; x++)
            {
                Vector2 spawnPosition = new Vector2(
                    startPos.x + (x * spacingX),
                    startPos.y + (y * spacingY)
                );

                GameObject newSlime = Instantiate(slimePrefab, spawnPosition, Quaternion.identity, transform);
                int cell = GameRules.ToIndex(x, y);
                cellViews[cell] = newSlime.GetComponent<SlimeController>();
                cellViews[cell].Init(cell);
            }
        }
    }

    public void ResetSlimeManager()
    {
        EndDestroySkill();
    }

    public void ShowHint()
    {
        if (cellViews == null) return;

        ClearAllHints();

        if (MatchFinder.FindFirst(board, GameManager.Instance.TargetSum, out BoardRect match))
        {
            foreach (int cell in match.GetCells())
            {
                if (board.GetSlimeAt(cell) != null)
                {
                    cellViews[cell].SetHintHighlight(true);
                }
            }
        }
    }

    public void ClearAllHints()
    {
        if (cellViews == null) return;

        foreach (SlimeController view in cellViews)
        {
            view.SetHintHighlight(false);
        }
    }

    public void MixSlimes()
    {
        if (cellViews == null) return;

        List<BoardSlime> slimes = new List<BoardSlime>();
        List<int> numbers = new List<int>();

        for (int cell = 0; cell < GameRules.CellCount; cell++)
        {
            BoardSlime slime = board.GetSlimeAt(cell);
            if (slime != null && slime.Anchor == cell)
            {
                slimes.Add(slime);
                numbers.Add(slime.Number);
            }
        }

        GameManager.Instance.SkillRandom.Shuffle(numbers);

        for (int i = 0; i < slimes.Count; i++)
        {
            board.SetNumber(slimes[i], numbers[i]);
        }
    }

    protected override void OnDestroy()
    {
        SubscribeBoard(false);
        base.OnDestroy();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}
