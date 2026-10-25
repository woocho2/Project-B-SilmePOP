// 기획에서 정한 숫자를 한곳에 모음. 규칙 숫자를 바꿀 때는 여기만 고침.
// 기획.txt의 어느 장인지 옆에 적어 둠.
public static class GameRules
{
    // ── 판 (기획 [4]) ──────────────────────────────────────────
    public const int Columns = 17;
    public const int Rows = 10;
    public const int CellCount = Columns * Rows;   // 170

    // ── 한 판 (기획 [4]) ───────────────────────────────────────
    public const float MatchDuration = 120f;
    public const int CountdownFrom = 3;          // 3, 2, 1
    public const float RevealDuration = 2f;      // 목표 합 공개 연출
    public const float StartDelay = 5f;          // 방장이 시작을 누른 뒤 startAt까지 (모두 씬을 불러올 시간)
    public const int MinTargetSum = 10;
    public const int MaxTargetSum = 15;          // 10 ~ 15 (둘 다 포함)

    // ── 특수 슬라임 출현 수 (기획 [4-2]). 나머지는 기본 ─────────
    public const int KingCount = 2;
    public const int AngelCount = 3;
    public const int DevilCount = 5;

    // ── 점수 (기획 [4-2]) ──────────────────────────────────────
    // 점수 = B + Σ천사(B x AngelMultiplier) + Σ악마(B x DevilMultiplier) + Σ킹(KingBonus) + Σ조커(목표 합 x JokerTargetMultiplier)
    public const int AngelMultiplier = 2;
    public const int DevilMultiplier = -1;
    public const int KingBonus = 10;
    public const int JokerTargetMultiplier = 2;
    public const int MergeZeroScorePerSlime = 4;   // 합체 결과가 0이라 바로 터지면 합체된 수 x 4

    // 종류별 기본점수. 합체는 합쳐진 슬라임들의 합이라 BoardSlime이 따로 들고 있음.
    public static int BaseScore(SlimeType type)
    {
        switch (type)
        {
            case SlimeType.Angel: return 2;
            case SlimeType.Joker: return 3;
            default: return 1;   // 기본, 악마, 킹
        }
    }

    // ── 스킬 (기획 [4-1]) ──────────────────────────────────────
    public const int TransformSize = 5;          // 변신 5 x 5
    public const int MergeSize = 2;              // 합체 2 x 2
    public const int MergeMinSlimes = 2;         // 2 x 2 안에 2마리 이상이면 사용 가능
    public const float JokerMoveInterval = 5f;

    public static int SkillUses(SkillType skill)
    {
        switch (skill)
        {
            case SkillType.Magnifier: return 1;
            case SkillType.Destroy: return 2;
            case SkillType.Transform: return 1;
            case SkillType.Merge: return 1;
            case SkillType.Joker: return 1;
            case SkillType.NumberChange: return 3;
            default: return 0;
        }
    }

    // ── 방과 네트워크 (기획 [6]) ───────────────────────────────
    public const int MaxPlayers = 4;             // 사람 + AI
    public const float HeartbeatInterval = 5f;   // 이 간격으로 "아직 있음"을 알림
    public const float DisconnectTimeout = 15f;  // 이만큼 소식이 없으면 끊긴 것 → 대전 중이면 기권
    public const float FinalBoardWait = 5f;      // 종료 뒤 최종 판을 기다리는 최대 시간
    public const int ChatMaxLength = 100;
    public const float ChatCooldown = 1f;        // 도배 방지: 1초에 1번

    // ── 칸 번호 ↔ 좌표 (구현 계획서 4장 [4-1]) ─────────────────
    // 칸 번호 = y * Columns + x. 네트워크와 스냅샷은 칸 번호로 보냄.
    public static int ToIndex(int x, int y) => y * Columns + x;
    public static int IndexToX(int index) => index % Columns;
    public static int IndexToY(int index) => index / Columns;
    public static bool IsInside(int x, int y) => x >= 0 && x < Columns && y >= 0 && y < Rows;
}
