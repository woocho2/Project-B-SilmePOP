using UnityEngine;

// 방장 PC에서 AI 자리 하나를 맡아 자기 판을 풂 (기획 [5]). 화면 없이 BoardModel만 씀 (SlimeManager는 내 판만 그림).
// 모두와 같은 시드로 만든 같은 판을 풀고, 점수만 서버에 올림. 경기가 진행 중(GameManager.IsPlaying)일 때만 움직임.
// 다른 PC에서도 만들면 점수가 두 번 올라가므로 GameManager가 방장일 때만 만듦.
// 발표용 자동 플레이(AiAutoPlay)에서는 내 판을 대신 풂: 사람처럼 사각형을 끌어 터뜨리고(DragManager.PlayAiDrag),
// 아이템은 범위를 보여 준 뒤 씀(SkillManager.AiUse). 점수·소리·서버 보고는 사람이 둔 것과 같은 길로 감.
// 무엇을 할지는 AiAgent(순수 C#)가 정하고, 여기는 시간을 재고 결과를 보여 주기만 함.
public class AiPlayer : MonoBehaviour
{
    private const float SkillPreviewTime = 0.6f;   // 자동 플레이: 아이템 범위를 보여 주는 시간

    private int slot;
    private AiAgent agent;
    private BoardModel board;
    private SkillSet skills;
    private int targetSum;
    private float waitLeft;
    private int score;
    private bool isMyBoard;   // 내 판을 대신 푸는 중. 판·스킬·목표 합은 재시작해도 따라가도록 그때그때 가져옴
    private float busyUntil;  // 자동 플레이: 끄는 중·아이템 범위를 보여 주는 중 (끝나면 0)

    // AI 자리의 번호와 판 (판이 끝나면 방장이 결과 화면용으로 올림). 내 판을 대신 푸는 AI는 -1 / null.
    public int Slot => isMyBoard ? -1 : slot;
    public BoardModel OwnBoard => isMyBoard ? null : board;

    public AiDifficulty Difficulty
    {
        get => agent.Difficulty;
        set
        {
            agent.Difficulty = value;
            name = isMyBoard ? $"AI AutoPlay ({value})" : $"AI {slot + 1} ({value})";
        }
    }

    public static AiPlayer Create(int slot, AiDifficulty difficulty, MatchSettings settings)
    {
        AiPlayer ai = new GameObject().AddComponent<AiPlayer>();
        ai.slot = slot;
        ai.agent = new AiAgent(difficulty, unchecked(settings.Seed * 397 + slot));
        ai.Difficulty = difficulty;
        ai.board = BoardGenerator.Generate(settings.Seed);
        ai.skills = new SkillSet(settings.Seed, settings.Skills);   // 사람과 같은 아이템만
        ai.targetSum = settings.TargetSum;
        ai.waitLeft = ai.agent.FirstWait(ai.targetSum);
        return ai;
    }

    // 자동 플레이: 화면에 보이는 내 판을 AI가 풂
    public static AiPlayer CreateForMyBoard(AiDifficulty difficulty)
    {
        AiPlayer ai = new GameObject().AddComponent<AiPlayer>();
        ai.isMyBoard = true;
        ai.slot = -1;
        ai.agent = new AiAgent(difficulty, System.Environment.TickCount) { PlanOnly = true };
        ai.Difficulty = difficulty;
        ai.waitLeft = ai.agent.FirstWait(ai.TargetSum);
        return ai;
    }

    private BoardModel Board => isMyBoard ? GameManager.Instance.Board : board;
    private SkillSet Skills => !isMyBoard ? skills : SkillManager.Instance != null ? SkillManager.Instance.Skills : null;
    private int TargetSum => isMyBoard ? GameManager.Instance.TargetSum : targetSum;

    private void Update()
    {
        GameManager game = GameManager.Instance;
        if (game == null || !game.IsPlaying || Board == null) return;

        if (!isMyBoard) board.Tick(Time.deltaTime);   // 조커 이동 (내 판의 조커는 GameManager가 움직임)
        waitLeft -= Time.deltaTime;                    // 끄는 동안에도 흐름 (반응 시간 안에 끄는 시간이 들어감)
        if (waitLeft > 0f || Time.time < busyUntil) return;

        AiAction action = agent.Act(Board, Skills, TargetSum, game.CurrentTime);
        waitLeft = action.Wait;

        if (isMyBoard)
        {
            ShowOnMyBoard(action);
            return;
        }

        if (action.Kind == AiActionKind.Move || action.Kind == AiActionKind.Skill) AddScore(game, action.Score);
    }

    // 자동 플레이: 고른 수를 화면에서 사람처럼 보여 준 뒤 적용 (적용·점수·소리는 DragManager·SkillManager가 함)
    private void ShowOnMyBoard(AiAction action)
    {
        if (action.Kind == AiActionKind.Move && DragManager.Instance != null && SlimeManager.Instance != null)
        {
            BoardRect rect = AiBrain.Tighten(Board, action.Rect);
            SlimeManager.Instance.RectToWorld(rect, out Vector2 start, out Vector2 end);
            if (Random.value < 0.35f) (start, end) = (end, start);   // 늘 같은 방향으로 끌지 않게

            // 멀리 끌수록 조금 더 오래. 다음 수까지의 반응 시간보다 길지 않게
            float duration = Mathf.Clamp(0.25f + 0.05f * Vector2.Distance(start, end), 0.25f, 0.6f);
            duration = Mathf.Min(duration, Mathf.Max(0.15f, action.Wait - 0.2f));
            busyUntil = Time.time + duration + 1f;
            DragManager.Instance.PlayAiDrag(start, end, rect, duration, () => busyUntil = 0f);
        }
        else if (action.Kind == AiActionKind.Skill && SkillManager.Instance != null)
        {
            busyUntil = Time.time + SkillPreviewTime + 1f;
            SkillManager.Instance.AiUse(action.Skill, action.Cell, SkillPreviewTime, () => busyUntil = 0f);
        }
    }

    private void AddScore(GameManager game, int gained)
    {
        if (gained == 0) return;
        score += gained;
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.ReportAiScore(slot, score, game.MaxGameTime - game.CurrentTime);
        }
    }
}
