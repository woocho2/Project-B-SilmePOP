using System;
using System.Collections.Generic;

// 네트워크 구현의 약속. 게임 코드는 NetworkManager를 통해 이것만 알면 되고,
// Local(서버 없이)과 Firebase(실제 서버) 중 무엇이 돌아가는지 몰라도 됨.
// 네트워크는 오래 걸리므로 결과는 반환값이 아니라 콜백과 이벤트로 받음.
public interface INetworkService
{
    event Action Connected;
    event Action Disconnected;
    event Action<string> ErrorOccurred;                       // 사람에게 보여 줄 실패 메시지
    event Action<IReadOnlyList<UserInfo>> LobbyUsersChanged;
    event Action<IReadOnlyList<RoomInfo>> RoomListChanged;
    event Action<RoomInfo> RoomChanged;                       // 지금 방이 바뀜. 방에서 나오면 null
    event Action<ChatChannel, ChatMessage> ChatReceived;
    event Action<MatchSettings> MatchStarting;                // 방장이 시작함. 모든 참가자가 받음
    event Action<IReadOnlyList<ScoreEntry>> ScoresChanged;    // 모든 자리의 점수
    event Action<int, BoardSnapshot> FinalBoardReceived;      // 자리, 그 자리의 최종 판
    event Action<int> HostChanged;                            // 새 방장의 자리

    // 서버 시각 (1970년부터의 밀리초). PC마다 시계가 다르므로 경기 시간은 이것으로만 잼.
    long ServerNow { get; }

    // 서버에 닿는지 확인. 로그인 전에도 부를 수 있음.
    void CheckConnection(Action<bool> onDone);

    // 로그인 창에 보여 줄 게스트 번호를 미리 받음. 로그인 전까지 다시 불러도 같은 번호. 실패하면 onDone(0).
    void ReserveGuestNumber(Action<int> onDone);

    // 로그인. 받아 둔 게스트 번호를 씀. 실패하면 onDone(null)과 ErrorOccurred.
    void SignIn(string nickname, Action<UserInfo> onDone);
    void SignOut();

    // 방 만들기와 들어가기. 실패하면 onDone(null)과 ErrorOccurred.
    void CreateRoom(string roomName, int maxPlayers, Action<RoomInfo> onDone);
    void JoinRoom(string roomId, Action<RoomInfo> onDone);
    void LeaveRoom();

    // 내 슬라임 색 "#RRGGBB". 방에 있으면 내 자리 색도 바꿔서 모두에게 알림.
    void SetMyColor(string colorHex);

    // 방장만. 이미 AI가 있는 자리에 AddAi를 다시 부르면 난이도만 바뀜.
    void AddAi(int slot, AiDifficulty difficulty);
    void RemoveAi(int slot);
    void StartMatch();
    void EndMatch();

    // 게임 씬을 다 불러왔음. 모두 오면(또는 너무 오래 걸리면) 방장 쪽에서 playAt이 정해짐.
    void ReportLoaded(int slot);

    void SendChat(ChatChannel channel, string text);

    // 내 자리는 내가, AI 자리는 방장이 올림.
    void ReportScore(int slot, int score, float lastScoreTime);
    void ReportForfeit(int slot, float forfeitTime);
    void UploadFinalBoard(int slot, BoardSnapshot snapshot);

    // 결과 화면에서 대기실로 돌아감. 그 판의 점수와 최종 판 기록을 지움.
    void ReturnToRoom();

    // 아직 접속해 있음을 알림. NetworkManager가 일정한 간격으로 부름.
    void SendHeartbeat();
}
