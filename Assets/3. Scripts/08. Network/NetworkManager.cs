using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

// 게임 코드와 네트워크 구현 사이의 창구. 씬을 넘어 유지됨.
// 서버에서 온 콜백은 메인 스레드가 아닐 수 있으므로 큐에 담았다가 Update에서 처리함.
// 그래서 이 클래스의 콜백과 이벤트는 항상 메인 스레드에서, 부른 다음 프레임 이후에 옴.
public class NetworkManager : Singleton<NetworkManager>
{
    private const string NicknameKey = "Network.Nickname";
    private const string EmailKey = "Network.Email";
    private const int ChatHistoryLimit = 100;

    [SerializeField] private NetworkBackend backend = NetworkBackend.Local;

    [Header("Firebase (Backend가 Firebase일 때)")]
    [Tooltip("Realtime Database 화면 맨 위의 주소. 예: https://프로젝트-default-rtdb.asia-southeast1.firebasedatabase.app")]
    [SerializeField] private string firebaseDatabaseUrl;
    [Tooltip("프로젝트 설정 → 일반 탭의 웹 API 키")]
    [SerializeField] private string firebaseApiKey;

    public event Action Connected;
    public event Action Disconnected;
    public event Action<string> ErrorOccurred;
    public event Action<IReadOnlyList<UserInfo>> LobbyUsersChanged;
    public event Action<IReadOnlyList<RoomInfo>> RoomListChanged;
    public event Action<RoomInfo> RoomChanged;
    public event Action<ChatChannel, ChatMessage> ChatReceived;
    public event Action<MatchSettings> MatchStarting;
    public event Action<MatchScores> ScoresChanged;
    public event Action<int, BoardSnapshot> FinalBoardReceived;
    public event Action<int> HostChanged;

    protected override bool PersistAcrossScenes => true;

    public UserInfo CurrentUser { get; private set; }
    public RoomInfo CurrentRoom { get; private set; }
    public MatchSettings CurrentMatch { get; private set; }   // 진행 중인 판. 없으면 null
    public MatchScores Scores { get; } = new MatchScores();
    public IReadOnlyList<RoomInfo> Rooms { get; private set; } = new RoomInfo[0];   // 마지막으로 받은 방 목록
    public IReadOnlyList<UserInfo> LobbyUsers { get; private set; } = new UserInfo[0];   // 마지막으로 받은 접속자 목록

    // 이번 판에 받은 최종 판 (자리 → 판). 결과 화면이 늦게 열려도 먼저 온 판을 볼 수 있게 들고 있음. 새 판이 시작되면 비움.
    public IReadOnlyDictionary<int, BoardSnapshot> FinalBoards => finalBoards;
    private readonly Dictionary<int, BoardSnapshot> finalBoards = new Dictionary<int, BoardSnapshot>();

    public bool IsConnected => CurrentUser != null;

    // 로그인한 상태면 NetworkManager, 아니면 경고를 남기고 null. 방 만들기·들어가기 전에 씀.
    public static NetworkManager SignedIn(UnityEngine.Object context)
    {
        if (Instance != null && Instance.IsConnected) return Instance;

        Debug.LogWarning("방에 들어가려면 타이틀에서 로그인해야 합니다.", context);
        return null;
    }
    public bool IsHost => CurrentUser != null && CurrentRoom != null && CurrentRoom.hostUid == CurrentUser.uid;
    public int MySlot => CurrentUser != null && CurrentRoom != null ? CurrentRoom.FindSlot(CurrentUser.uid) : -1;
    public long ServerNow => service.ServerNow;

    // 지난 접속 때의 닉네임·이메일. 로그인 입력칸에 미리 채움 (비밀번호는 저장하지 않음).
    public string SavedNickname => PlayerPrefs.GetString(NicknameKey, "");
    public string SavedEmail => PlayerPrefs.GetString(EmailKey, "");

    // 받은 채팅 기록. 씬이 바뀌어 채팅창이 새로 생겨도 지난 대화를 다시 보여 주려고 들고 있음.
    private readonly List<KeyValuePair<ChatChannel, ChatMessage>> chatHistory = new List<KeyValuePair<ChatChannel, ChatMessage>>();

    private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
    private INetworkService service;
    private float heartbeatTimer;
    private float lastChatTime = float.NegativeInfinity;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        service = CreateService();
        SubscribeService();

        // 메뉴에서 고른 슬라임 색을 대기실의 내 자리에 보여 줌
        StyleSettings.ColorChanged += SendMyColor;
        SendMyColor(StyleSettings.SelectedColor);
    }

    protected override void OnDestroy()
    {
        StyleSettings.ColorChanged -= SendMyColor;
        base.OnDestroy();
    }

    private void SendMyColor(Color color)
    {
        service.SetMyColor("#" + ColorUtility.ToHtmlStringRGB(color));
    }

    private INetworkService CreateService()
    {
        if (backend == NetworkBackend.Firebase)
        {
            if (!string.IsNullOrWhiteSpace(firebaseDatabaseUrl) && !string.IsNullOrWhiteSpace(firebaseApiKey))
            {
                return new FirebaseNetworkService(new FirebaseRestClient(firebaseDatabaseUrl, firebaseApiKey));
            }
            Debug.LogError("NetworkManager: Firebase 주소와 웹 API 키를 넣어 주세요. 지금은 Local로 동작합니다.", this);
        }
        return new LocalNetworkService();
    }

    // 끌 때 방에서 나가고 실시간 수신을 닫음 (에디터에서는 플레이를 멈춰도 연결이 남을 수 있음)
    private void OnApplicationQuit()
    {
        if (service != null && IsConnected) service.SignOut();
    }

    private void SubscribeService()
    {
        service.Connected += () => Post(() => Connected?.Invoke());
        service.Disconnected += () => Post(HandleDisconnected);
        service.ErrorOccurred += message => Post(() => ErrorOccurred?.Invoke(message));
        service.LobbyUsersChanged += users => Post(() =>
        {
            LobbyUsers = users;
            LobbyUsersChanged?.Invoke(users);
        });
        service.RoomListChanged += rooms => Post(() =>
        {
            Rooms = rooms;
            RoomListChanged?.Invoke(rooms);
        });
        service.RoomChanged += room => Post(() => HandleRoomChanged(room));
        service.ChatReceived += (channel, message) => Post(() => AddChat(channel, message));
        service.MatchStarting += settings => Post(() => HandleMatchStarting(settings));
        service.ScoresChanged += entries => Post(() => HandleScoresChanged(entries));
        service.FinalBoardReceived += (slot, snapshot) => Post(() =>
        {
            if (snapshot == null) return;
            finalBoards[slot] = snapshot;
            FinalBoardReceived?.Invoke(slot, snapshot);
        });
        service.HostChanged += slot => Post(() => HostChanged?.Invoke(slot));
    }

    private void Update()
    {
        while (mainThreadQueue.TryDequeue(out Action action))
        {
            action();
        }

        if (!IsConnected) return;

        heartbeatTimer += Time.unscaledDeltaTime;
        if (heartbeatTimer >= GameRules.HeartbeatInterval)
        {
            heartbeatTimer = 0f;
            service.SendHeartbeat();
        }
    }

    private void Post(Action action)
    {
        mainThreadQueue.Enqueue(action);
    }

    private void HandleDisconnected()
    {
        CurrentUser = null;
        CurrentRoom = null;
        CurrentMatch = null;
        Scores.Clear();
        finalBoards.Clear();
        LobbyUsers = new UserInfo[0];
        Rooms = new RoomInfo[0];
        chatHistory.Clear();
        Disconnected?.Invoke();
    }

    private void HandleRoomChanged(RoomInfo room)
    {
        RoomInfo previous = CurrentRoom;
        CurrentRoom = room;
        if (room == null || room.state == RoomState.Waiting)
        {
            CurrentMatch = null;
            Scores.Clear();
        }

        // 다른 방이면 지난 방의 대화를 지우고, 같은 방이면 누가 들어오고 나갔는지 방 채팅에 알림
        if (previous == null || room == null || previous.roomId != room.roomId)
        {
            chatHistory.RemoveAll(entry => entry.Key != ChatChannel.Lobby);
        }
        RoomChanged?.Invoke(room);
        if (previous != null && room != null && previous.roomId == room.roomId) AnnounceRoomChanges(previous, room);
    }

    // 자리마다 이전과 비교: 빈자리에 누가 오면 입장, 있던 사람이 빠지면 퇴장, 방장이 바뀌면 알림
    private void AnnounceRoomChanges(RoomInfo previous, RoomInfo room)
    {
        for (int i = 0; i < room.slots.Length && i < previous.slots.Length; i++)
        {
            SlotInfo before = previous.slots[i];
            SlotInfo after = room.slots[i];
            bool sameOccupant = before.kind == after.kind && before.uid == after.uid && before.displayName == after.displayName;
            if (sameOccupant) continue;

            if (before.kind != SlotKind.Empty) ShowSystemMessage(ChatChannel.Room, $"{SlotLabel(before)}님이 퇴장했습니다.");
            if (after.kind != SlotKind.Empty) ShowSystemMessage(ChatChannel.Room, $"{SlotLabel(after)}님이 입장했습니다.");
        }

        if (previous.hostUid != room.hostUid)
        {
            int hostSlot = room.FindSlot(room.hostUid);
            if (hostSlot >= 0) ShowSystemMessage(ChatChannel.Room, $"{SlotLabel(room.slots[hostSlot])}님이 방장이 되었습니다.");
        }
    }

    private static string SlotLabel(SlotInfo slot)
    {
        return slot.kind == SlotKind.Ai ? $"AI {slot.displayName}" : slot.displayName;
    }

    private void AddChat(ChatChannel channel, ChatMessage message)
    {
        chatHistory.Add(new KeyValuePair<ChatChannel, ChatMessage>(channel, message));
        if (chatHistory.Count > ChatHistoryLimit) chatHistory.RemoveAt(0);
        ChatReceived?.Invoke(channel, message);
    }

    // 지금까지 받은 채팅 (오래된 순). 채팅창이 새로 생길 때 다시 보여 주는 데 씀.
    public List<ChatMessage> ChatHistory(ChatChannel channel)
    {
        List<ChatMessage> messages = new List<ChatMessage>();
        foreach (KeyValuePair<ChatChannel, ChatMessage> entry in chatHistory)
        {
            if (entry.Key == channel) messages.Add(entry.Value);
        }
        return messages;
    }

    private void HandleMatchStarting(MatchSettings settings)
    {
        CurrentMatch = settings;
        finalBoards.Clear();
        MatchStarting?.Invoke(settings);
    }

    private void HandleScoresChanged(IReadOnlyList<ScoreEntry> entries)
    {
        Scores.SetEntries(entries);
        ScoresChanged?.Invoke(Scores);
    }

    public void CheckConnection(Action<bool> onDone)
    {
        service.CheckConnection(isConnected => Post(() => onDone?.Invoke(isConnected)));
    }

    // 회원가입 창에 미리 채울 닉네임. 지난번 닉네임이 없으면 성격 + 색깔 + 슬라임을 무작위로 조합.
    public string SuggestNickname()
    {
        string saved = SavedNickname;
        if (!string.IsNullOrWhiteSpace(saved)) return saved;

        return GameRules.RandomNickname(new System.Random());
    }

    // 이메일 회원가입. 성공하면 바로 로그인된 상태가 됨.
    public void SignUpWithEmail(string email, string password, string nickname, Action<UserInfo> onDone = null)
    {
        service.SignUpWithEmail(email, password, nickname, user => Post(() => FinishSignIn(user, email, onDone)));
    }

    // 이메일 로그인. 실패 이유는 ErrorOccurred로 옴.
    public void SignInWithEmail(string email, string password, Action<UserInfo> onDone = null)
    {
        service.SignInWithEmail(email, password, user => Post(() => FinishSignIn(user, email, onDone)));
    }

    private void FinishSignIn(UserInfo user, string email, Action<UserInfo> onDone)
    {
        CurrentUser = user;
        if (user != null)
        {
            PlayerPrefs.SetString(NicknameKey, user.nickname);
            if (!string.IsNullOrWhiteSpace(email)) PlayerPrefs.SetString(EmailKey, email.Trim());
        }
        onDone?.Invoke(user);
    }

    // 로그아웃: 방에서 나가고 접속을 끊음. 끊기면 Disconnected가 옴.
    public void SignOut() => service.SignOut();

    // 방 이름이 비어 있으면 "닉네임의 방". 너무 길면 자름.
    public void CreateRoom(string roomName, int maxPlayers, Action<RoomInfo> onDone = null)
    {
        string trimmed = string.IsNullOrWhiteSpace(roomName) ? "" : roomName.Trim();
        if (trimmed.Length == 0 && CurrentUser != null) trimmed = $"{CurrentUser.nickname}의 방";
        if (trimmed.Length > GameRules.RoomNameMaxLength) trimmed = trimmed.Substring(0, GameRules.RoomNameMaxLength);

        service.CreateRoom(trimmed, maxPlayers, room => Post(() => onDone?.Invoke(room)));
    }

    public void JoinRoom(string roomId, Action<RoomInfo> onDone = null)
    {
        service.JoinRoom(roomId, room => Post(() => onDone?.Invoke(room)));
    }

    public void LeaveRoom() => service.LeaveRoom();
    public void AddAi(int slot, AiDifficulty difficulty) => service.AddAi(slot, difficulty);
    public void RemoveAi(int slot) => service.RemoveAi(slot);
    public void StartMatch() => service.StartMatch();
    public void EndMatch() => service.EndMatch();

    // 게임 씬을 다 불러왔음. 모두 오면 CurrentRoom.playAt이 정해짐.
    public void ReportLoaded()
    {
        if (MySlot >= 0) service.ReportLoaded(MySlot);
    }

    // 이 PC에서만 보이는 안내 메시지를 채팅으로 띄움 (서버로 보내지 않음)
    public void ShowSystemMessage(ChatChannel channel, string text)
    {
        AddChat(channel, new ChatMessage { text = text, time = ServerNow });
    }
    public void ReturnToRoom() => service.ReturnToRoom();

    // 빈 글이거나 도배 제한에 걸리면 보내지 않고 false. 너무 길면 잘라서 보냄.
    public bool SendChat(ChatChannel channel, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (Time.unscaledTime - lastChatTime < GameRules.ChatCooldown) return false;
        if (channel != ChatChannel.Lobby && CurrentRoom == null)
        {
            Debug.LogWarning($"방에 들어가 있지 않아서 {channel} 채팅을 보낼 수 없습니다. 로비 채팅창이면 ChatPanel의 Channel을 Lobby로 바꾸세요.");
            return false;
        }

        lastChatTime = Time.unscaledTime;
        string trimmed = text.Trim();
        if (trimmed.Length > GameRules.ChatMaxLength)
        {
            trimmed = trimmed.Substring(0, GameRules.ChatMaxLength);
        }
        service.SendChat(channel, trimmed);
        return true;
    }

    public void ReportMyScore(int score, float lastScoreTime)
    {
        if (MySlot >= 0) service.ReportScore(MySlot, score, lastScoreTime);
    }

    // AI 자리의 점수. 방장 PC의 AiPlayer만 부름.
    public void ReportAiScore(int slot, int score, float lastScoreTime)
    {
        if (IsHost) service.ReportScore(slot, score, lastScoreTime);
    }

    public void ReportMyForfeit(float forfeitTime)
    {
        if (MySlot >= 0) service.ReportForfeit(MySlot, forfeitTime);
    }

    public void UploadFinalBoard(int slot, BoardSnapshot snapshot)
    {
        service.UploadFinalBoard(slot, snapshot);
    }
}
