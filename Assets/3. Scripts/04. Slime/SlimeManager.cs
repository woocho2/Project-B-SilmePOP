using DG.Tweening;
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

    // 칸의 월드 좌표 (칸 가운데)
    public Vector2 CellToWorld(int cell)
    {
        return GridOrigin + new Vector2(GameRules.IndexToX(cell) * Spacing.x, GameRules.IndexToY(cell) * Spacing.y);
    }

    // 사각형을 덮는 드래그의 시작·끝 월드 좌표 (왼쪽 위 → 오른쪽 아래). 옆 칸 슬라임에는 닿지 않을 만큼만 바깥으로.
    public void RectToWorld(BoardRect rect, out Vector2 start, out Vector2 end)
    {
        Vector2 pad = Spacing * 0.35f;
        Vector2 min = CellToWorld(GameRules.ToIndex(rect.MinX, rect.MinY)) - pad;
        Vector2 max = CellToWorld(GameRules.ToIndex(rect.MaxX, rect.MaxY)) + pad;
        start = new Vector2(min.x, max.y);
        end = new Vector2(max.x, min.y);
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

    // 터짐·파괴: 슬라임마다 조금씩 시간차를 두고 톡톡 터지는 연출 뒤에 빈칸으로
    private void OnSlimesRemoved(IReadOnlyList<BoardSlime> slimes)
    {
        for (int i = 0; i < slimes.Count; i++)
        {
            BoardSlime slime = slimes[i];
            int anchor = slime.Anchor;
            foreach (int cell in slime.Cells)
            {
                if (cell != anchor) RefreshCell(cell);   // 합체 슬라임의 나머지 칸은 그림이 없으므로 바로
            }
            cellViews[anchor].PlayPop(i * PopStagger, () => RefreshCell(anchor));
        }
    }

    private const float PopStagger = 0.035f;

    // ── 점수 글자 ──────────────────────────────────────────────

    [Header("점수 글자 (+12)")]
    [SerializeField] private float scorePopupSize = 5f;

    // 칸들의 가운데에 "+점수"를 띄움 (터뜨린 조합, 파괴, 합체로 얻은 점수)
    public void ShowScore(IEnumerable<int> cells, int score)
    {
        if (cellViews == null) return;

        Vector2 sum = Vector2.zero;
        int count = 0;
        foreach (int cell in cells)
        {
            if (cell < 0 || cell >= cellViews.Length) continue;
            sum += CellToWorld(cell);
            count++;
        }
        if (count == 0) return;

        SlimeController sample = cellViews[0];
        ScorePopup.Show(sum / count, score, scorePopupSize, sample.NumberFont, sample.Body);
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

    // 돋보기: 알려 줄 조합(MagnifierSkill.LastHint)을 노란 외곽선으로 표시
    public void ShowHint(BoardRect match)
    {
        if (cellViews == null) return;

        ClearAllHints();
        foreach (int cell in match.GetCells())
        {
            if (board.GetSlimeAt(cell) != null)
            {
                cellViews[cell].SetHintHighlight(true);
            }
        }
    }

    // 조커: 어디가 조커가 됐는지 잠깐 외곽선으로 알려 줌
    public void FlashCell(int cell, Color color, float seconds)
    {
        if (cellViews == null || cell < 0 || cell >= cellViews.Length) return;

        int[] cells = { cell };
        SetSkillPreview(cells, true, color);
        DOVirtual.DelayedCall(seconds, () => SetSkillPreview(cells, false, Color.clear)).SetLink(gameObject);
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
