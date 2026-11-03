using UnityEngine;

// 방장 PC에서 AI 자리 하나를 맡아 자기 판을 풂 (기획 [5]). 화면 없이 BoardModel만 씀 (SlimeManager는 내 판만 그림).
// 모두와 같은 시드로 만든 같은 판을 풀고, 점수만 서버에 올림. 경기가 진행 중(GameManager.IsPlaying)일 때만 움직임.
// 다른 PC에서도 만들면 점수가 두 번 올라가므로 GameManager가 방장일 때만 만듦.
public class AiPlayer : MonoBehaviour
{
    private const float BurstChance = 0.15f;      // 가끔 연속으로 빠르게 터뜨림 (사람처럼 보이게)
    private const float BurstFactor = 0.4f;
    private const float PerSlimeDelay = 0.08f;    // 큰 사각형일수록 조금 더 오래 걸림
    private const float StuckRetry = 1f;          // 막혔을 때 다시 볼 간격 (조커가 움직이면 수가 생길 수 있음)

    private int slot;
    private AiDifficulty difficulty;
    private BoardModel board;
    private SkillSet skills;
    private int targetSum;
    private System.Random random;
    private float waitLeft;
    private int score;

    public static AiPlayer Create(int slot, AiDifficulty difficulty, MatchSettings settings)
    {
        AiPlayer ai = new GameObject($"AI {slot + 1} ({difficulty})").AddComponent<AiPlayer>();
        ai.slot = slot;
        ai.difficulty = difficulty;
        ai.board = BoardGenerator.Generate(settings.Seed);
        ai.skills = new SkillSet(settings.Seed);
        ai.targetSum = settings.TargetSum;
        ai.random = new System.Random(unchecked(settings.Seed * 397 + slot));
        ai.waitLeft = ai.NextWait(1);
        return ai;
    }

    private void Update()
    {
        GameManager game = GameManager.Instance;
        if (game == null || !game.IsPlaying || board == null) return;

        board.Tick(Time.deltaTime);   // 조커 이동
        waitLeft -= Time.deltaTime;
        if (waitLeft > 0f) return;

        Act(game);
    }

    // 둘 수가 있으면 두고, 막혔으면 난이도별 스킬을 써 보고, 그것도 없으면 잠시 뒤 다시 봄
    private void Act(GameManager game)
    {
        if (AiBrain.TryChooseMove(board, targetSum, difficulty, random, out var move))
        {
            int gained = ScoreCalculator.Calculate(move, targetSum);
            board.Remove(move);
            AddScore(game, gained);
            waitLeft = NextWait(move.Count);
            return;
        }

        if (AiBrain.TryChooseRescue(board, targetSum, difficulty, skills, random, out SkillType skill, out int cell)
            && skills.TryUse(skill, board, cell, targetSum, out int skillScore))
        {
            if (skillScore != 0) AddScore(game, skillScore);
            waitLeft = NextWait(1);
            return;
        }

        waitLeft = StuckRetry;
    }

    private void AddScore(GameManager game, int gained)
    {
        score += gained;
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.ReportAiScore(slot, score, game.MaxGameTime - game.CurrentTime);
        }
    }

    // 다음 행동까지 기다릴 시간: 난이도별 반응 시간 + 큰 사각형 보정, 가끔 빠르게
    private float NextWait(int slimeCount)
    {
        float min = AiDifficultyTable.MinReaction(difficulty);
        float max = AiDifficultyTable.MaxReaction(difficulty);
        float wait = min + (float)random.NextDouble() * (max - min);
        wait += PerSlimeDelay * Mathf.Max(0, slimeCount - 2);
        if (random.NextDouble() < BurstChance) wait *= BurstFactor;
        return wait;
    }
}
