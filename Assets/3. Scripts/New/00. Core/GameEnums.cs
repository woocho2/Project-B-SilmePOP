// 게임 전체에서 쓰는 enum 모음.
// 숫자를 직접 적어 둠: 인스펙터·네트워크에는 이름이 아니라 숫자로 저장되므로, 중간에 끼워 넣으면 저장된 값이 밀림.
// 새 값은 항상 맨 뒤에 더함.

// 슬라임 종류 (기획 [4-2]). 빈칸은 enum이 아니라 null(슬라임 없음)로 나타냄.
public enum SlimeType
{
    Basic = 0,   // 기본: 기본점수 1, 특수 없음
    Devil = 1,   // 악마: 기본점수 1, B x (-1)
    Angel = 2,   // 천사: 기본점수 2, B x 2
    King = 3,    // 킹:   기본점수 1, +10
    Joker = 4,   // 조커: 기본점수 3, +(목표 합 x 2). 스킬로만 생김
    Merged = 5   // 합체: 합쳐진 기본점수 합, 특수 없음. 스킬로만 생기고 2 x 2 칸을 차지
}

// 스킬 6종 (기획 [4-1]). 숫자 = 단축키 번호 (1 ~ 6).
public enum SkillType
{
    None = 0,
    Magnifier = 1,     // 돋보기
    Destroy = 2,       // 파괴
    Transform = 3,     // 변신 (5 x 5, 각각 +1 / -1)
    Merge = 4,         // 합체 (2 x 2)
    Joker = 5,         // 조커
    NumberChange = 6   // 숫자변경
}

// AI 난이도 (기획 [5]). 반응 시간·정답확률 표는 AiDifficultyTable.
public enum AiDifficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Test = 3   // 게임판 확인용 (0.5 ~ 1초, 100%)
}

// 한 판의 진행 단계. GameManager가 이 순서로만 넘어감.
public enum MatchPhase
{
    Waiting = 0,     // 시작 시각(startAt)을 기다리는 중
    Reveal = 1,      // 목표 합 공개 연출
    Countdown = 2,   // 3, 2, 1
    Playing = 3,     // 120초
    Ended = 4        // 입력 막음, 최종 판 모으기, 결과
}

// 대기실 자리 한 칸에 누가 있는가.
public enum SlotKind
{
    Empty = 0,
    Human = 1,
    Ai = 2
}

// 채팅이 어디로 가는가. 로비 채팅은 접속자 전체, 나머지는 같은 방 안.
public enum ChatChannel
{
    Lobby = 0,
    Room = 1,    // 대기실
    Match = 2,   // 대전 중
    Result = 3   // 결과 화면
}
