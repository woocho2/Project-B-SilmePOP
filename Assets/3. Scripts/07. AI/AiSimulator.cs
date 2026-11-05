using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// 화면 없이 한 판(GameRules.MatchDuration)을 AI 혼자 끝까지 둠 (AiPlayer와 같은 순서: 조커 이동 → 기다림 → 행동). 순수 C#.
// 개발용 테스트 모드에서 "내 판과 같은 시드로 AI는 몇 점?"을 재고, 테스트에서 난이도 차이를 확인할 때 씀.
public static class AiSimulator
{
    private const float Step = 0.05f;   // 조커가 움직일 때를 놓치지 않을 만큼 잘게

    public struct Result
    {
        public int Score;
        public int Removed;   // 비워진 칸 수 = 없앤 슬라임 수 (합체 슬라임은 4칸)
    }

    // 경기 시드가 같으면 판·아이템이 사람과 같음. targetSum이 0이면 시드로 정해지는 목표 합 (개발용 F1로 바꿨으면 그 값을 줌).
    // aiSeed는 AI의 반응 시간·실수 난수.
    public static Result Play(int matchSeed, AiDifficulty difficulty, int aiSeed, int targetSum = 0)
    {
        MatchSettings settings = targetSum > 0
            ? new MatchSettings(matchSeed, targetSum, 0L, 1)
            : MatchSettings.CreateNew(0L, 1, matchSeed);
        BoardModel board = BoardGenerator.Generate(matchSeed);
        SkillSet skills = new SkillSet(matchSeed, settings.Skills);
        AiAgent agent = new AiAgent(difficulty, aiSeed);

        int score = 0;
        float wait = agent.FirstWait(settings.TargetSum);
        for (float time = 0f; time < GameRules.MatchDuration; time += Step)
        {
            board.Tick(Step);
            wait -= Step;
            if (wait > 0f) continue;

            AiAction action = agent.Act(board, skills, settings.TargetSum, GameRules.MatchDuration - time);
            score += action.Score;
            wait = action.Wait;
        }
        return new Result { Score = score, Removed = CountRemoved(board) };
    }

    // 같은 판을 AI 난수만 바꿔 games번 (여러 스레드로). progress[0]에 끝난 판 수를 셈.
    public static Result[] PlayMany(int matchSeed, AiDifficulty difficulty, int games, int[] progress = null, int targetSum = 0)
    {
        Result[] results = new Result[games];
        Parallel.For(0, games, i =>
        {
            results[i] = Play(matchSeed, difficulty, unchecked(matchSeed * 397 + i + 1), targetSum);
            if (progress != null) Interlocked.Increment(ref progress[0]);
        });
        return results;
    }

    public static int CountRemoved(BoardModel board)
    {
        int removed = 0;
        for (int cell = 0; cell < GameRules.CellCount; cell++)
        {
            if (board.GetSlimeAt(cell) == null) removed++;
        }
        return removed;
    }
}
