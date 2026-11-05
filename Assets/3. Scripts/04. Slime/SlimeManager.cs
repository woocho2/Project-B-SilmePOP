using UnityEngine;
using System.Collections.Generic;

// 내 판의 화면. BoardModel의 이벤트를 듣고 칸을 다시 그리기만 함.
// 칸 좌표 ↔ 월드 좌표도 여기서 계산함 (스킬 대상 칸 고르기, 합체 슬라임 배치).
public class SlimeManager : Singleton<SlimeManager>
{
    [Header("Slime Settings")]
    [SerializeField] GameObject slimePrefab;

    [Header("Grid Settings")]
    [SerializeField] Vector2 areaSize = new Vector2(10f, 6f);

    private SlimeController[] cellViews;
    private BoardModel board;

    // 왼쪽 아래 칸(0, 0)의 위치와 칸 사이 간격
    private Vector2 GridOrigin => (Vector2)transform.position - areaSize / 2f;
    private Vector2 Spacing => new Vector2(areaSize.x / (GameRules.Columns - 1), areaSize.y / (GameRules.Rows - 1));

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

    // 월드 좌표에서 가장 가까운 칸. 판 밖이면 -1.
    public int CellAt(Vector2 worldPosition)
    {
        Vector2 local = worldPosition - GridOrigin;
        int x = Mathf.RoundToInt(local.x / Spacing.x);
        int y = Mathf.RoundToInt(local.y / Spacing.y);
        return GameRules.IsInside(x, y) ? GameRules.ToIndex(x, y) : -1;
    }

    // 스킬 범위 미리보기 (가능하면 초록, 불가면 빨강 외곽선)
    public void SetSkillPreview(IReadOnlyList<int> cells, bool isActive, Color color)
    {
        if (cellViews == null) return;

        foreach (int cell in cells)
        {
            if (cell >= 0 && cell < cellViews.Length) cellViews[cell].SetSkillPreview(isActive, color);
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
        BoardSlime slime = board.GetSlimeAt(cell);
        cellViews[cell].Refresh(slime);

        // 합체 슬라임은 왼쪽 아래(Anchor) 칸이 네 칸 가운데에 두 배 크기로 그림
        bool isMergedAnchor = slime != null && slime.Type == SlimeType.Merged && slime.Anchor == cell;
        cellViews[cell].SetMergedLayout(isMergedAnchor, Spacing / 2f);
    }

    private void GenerateCellViews()
    {
        if (slimePrefab == null)
        {
            Debug.LogError("슬라임 프리팹이 할당되지 않았습니다.");
            return;
        }

        cellViews = new SlimeController[GameRules.CellCount];
        for (int y = 0; y < GameRules.Rows; y++)
        {
            for (int x = 0; x < GameRules.Columns; x++)
            {
                Vector2 spawnPosition = GridOrigin + new Vector2(x * Spacing.x, y * Spacing.y);
                GameObject newSlime = Instantiate(slimePrefab, spawnPosition, Quaternion.identity, transform);
                int cell = GameRules.ToIndex(x, y);
                cellViews[cell] = newSlime.GetComponent<SlimeController>();
                cellViews[cell].Init(cell);
            }
        }
    }

    public void ResetSlimeManager()
    {
        ClearAllHints();
    }

    // 돋보기: 가능한 조합 1개를 노란 외곽선으로 표시 (악마 없는 조합 먼저, MagnifierSkill.FindHint)
    public void ShowHint()
    {
        if (cellViews == null) return;

        ClearAllHints();

        if (MagnifierSkill.FindHint(board, GameManager.Instance.TargetSum, out BoardRect match))
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
