// AI가 수와 아이템을 고르는 기준값. 모든 난이도가 같은 전략을 쓰고, 정확도와 반응 시간만 다름 (AiDifficultyTable).
// Best는 시뮬레이션(지옥 속도, 정확도 100%, 504판 탐색 + 새 시드 200판 검증)으로 평균 점수가 가장 높았던 값.
//   가장 큰 차이는 CountWeight: 슬라임을 적게 쓰는 수(큰 숫자부터)를 고르면 작은 숫자가 남아 판이 늦게 막힘
//   (평균 제거 129 → 150마리, 점수 187 → 208점). 한 수 앞 보기는 거의 차이가 없어 끔.
//   아이템 기준값은 차이가 작음 (어떻게든 판 안에 다 쓰게 됨). 하드·지옥에서 근소하게 좋았던 값.
public sealed class AiStrategy
{
    // ── 수 고르기 ──
    public float CountWeight = -3f;      // 슬라임 1마리당 가산점 (음수: 같은 점수면 적게 쓰는 수)
    public int Lookahead;                // 점수가 높은 수 몇 개를 한 수 앞까지 볼지 (0이면 안 봄)
    public float FutureWeight;           // 둔 뒤 남는 조합 수의 가치 (log(1 + 남는 조합 수) x 이 값)

    // ── 아이템 (판이 막히지 않았을 때 이만큼 이득이어야 씀. 점수로 환산한 값) ──
    public float JokerThreshold = 20f;
    public float MergeThreshold = 12f;
    public float DestroyThreshold = 12f;
    public float NumberChangeThreshold = 6f;

    // ── 막판 (아이템을 남기면 손해이므로 기준을 낮춤) ──
    public float EndgameTime = 20f;
    public float EndgameFactor = 0.3f;

    public static readonly AiStrategy Best = new AiStrategy();

    public AiStrategy Clone() => (AiStrategy)MemberwiseClone();
}
