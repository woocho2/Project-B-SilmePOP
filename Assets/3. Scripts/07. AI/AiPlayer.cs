using UnityEngine;

// 방장 PC에서 AI 자리 하나를 맡아 자기 판을 풂 (기획 [5]). 화면 없이 BoardModel만 씀 (SlimeManager는 내 판만 그림).
// 모두와 같은 시드로 만든 같은 판을 풀고, 점수만 서버에 올림. 경기가 진행 중(GameManager.IsPlaying)일 때만 움직임.
// 다른 PC에서도 만들면 점수가 두 번 올라가므로 GameManager가 방장일 때만 만듦.
// 개발용 테스트 모드(DebugCheat)에서는 내 판을 대신 풂: 화면에 그려지고 점수·소리는 사람이 둔 것과 같음.
// 무엇을 할지는 AiAgent(순수 C#)가 정하고, 여기는 시간을 재고 결과를 점수·소리로 알리기만 함.
public class AiPlayer : MonoBehaviour
{
    private int slot;
    private AiAgent agent;
    private BoardModel board;
    private SkillSet skills;
    private int targetSum;
    private float waitLeft;
    private int score;
    private bool isMyBoard;   // 내 판을 대신 푸는 중. 판·스킬·목표 합은 재시작해도 따라가도록 그때그때 가져옴

    public event System.Action<SkillType, int> SkillUsed;   // 스킬, 칸 (테스트 모드에서 보여 주기용)

    public AiDifficulty Difficulty
    {
        get => agent.Difficulty;
        set
        {
            agent.Difficulty = value;
            name = isMyBoard ? $"AI Test ({value})" : $"AI {slot + 1} ({value})";
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

    // 개발용 테스트 모드: 화면에 보이는 내 판을 AI가 풂
    public static AiPlayer CreateForMyBoard(AiDifficulty difficulty)
    {
        AiPlayer ai = new GameObject().AddComponent<AiPlayer>();
        ai.isMyBoard = true;
        ai.slot = -1;
        ai.agent = new AiAgent(difficulty, System.Environment.TickCount);
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
        waitLeft -= Time.deltaTime;
        if (waitLeft > 0f) return;

        AiAction action = agent.Act(Board, Skills, TargetSum, game.CurrentTime);
        waitLeft = action.Wait;

        if (action.Kind == AiActionKind.Move)
        {
            AddScore(game, action.Score, SoundEffect.Match);
        }
        else if (action.Kind == AiActionKind.Skill)
        {
            SkillUsed?.Invoke(action.Skill, action.Cell);
            AddScore(game, action.Score, SkillSound(action.Skill));
            if (isMyBoard && action.Skill == SkillType.Magnifier && SlimeManager.Instance != null) SlimeManager.Instance.ShowHint();
        }
    }

    private static SoundEffect SkillSound(SkillType skill)
    {
        switch (skill)
        {
            case SkillType.Destroy: return SoundEffect.Destroy;
            case SkillType.Magnifier: return SoundEffect.Hint;
            default: return SoundEffect.Mix;
        }
    }

    private void AddScore(GameManager game, int gained, SoundEffect sound)
    {
        if (isMyBoard)
        {
            // 사람이 둔 것처럼 (DragManager, SkillManager와 같은 순서)
            SoundManager.Play(sound);
            if (gained != 0) game.AddScore(gained);
            game.CheckForMoves();
            return;
        }

        if (gained == 0) return;
        score += gained;
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.ReportAiScore(slot, score, game.MaxGameTime - game.CurrentTime);
        }
    }
}
