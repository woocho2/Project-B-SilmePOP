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

    // 지난 접속 때의 닉네임. 로그인 입력칸에 미리 채움.
    public string SavedNickname => PlayerPrefs.GetString(NicknameKey, "");

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
        ChangeData.ColorChanged += SendMyColor;
        SendMyColor(ChangeData.SelectedColor);
    }

    protected override void OnDestroy()
    {
        ChangeData.ColorChanged -= SendMyColor;
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
        service.LobbyUsersChanged += users => Post(() => LobbyUsersChanged?.Invoke(users));
        service.RoomListChanged += rooms => Post(() =>
        {
            Rooms = rooms;
            RoomListChanged?.Invoke(rooms);
        });
        service.RoomChanged += room => Post(() => HandleRoomChanged(room));
        service.ChatReceived += (channel, message) => Post(() => ChatReceived?.Invoke(channel, message));
        service.MatchStarting += settings => Post(() => HandleMatchStarting(settings));
        service.ScoresChanged += entries => Post(() => HandleScoresChanged(entries));
        service.FinalBoardReceived += (slot, snapshot) => Post(() => FinalBoardReceived?.Invoke(slot, snapshot));
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
        Disconnected?.Invoke();
    }

    private void HandleRoomChanged(RoomInfo room)
    {
        CurrentRoom = room;
        if (room == null || room.state == RoomState.Waiting)
        {
            CurrentMatch = null;
            Scores.Clear();
        }
        RoomChanged?.Invoke(room);
    }

    private void HandleMatchStarting(MatchSettings settings)
    {
        CurrentMatch = settings;
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

    public void ReserveGuestNumber(Action<int> onDone)
    {
        service.ReserveGuestNumber(number => Post(() => onDone?.Invoke(number)));
    }

    // 로그인 창에 미리 채울 닉네임. 지난번 닉네임이 없으면 성격 + 색깔 + 슬라임을 무작위로 조합.
    public string SuggestNickname()
    {
        string saved = SavedNickname;
        if (!string.IsNullOrWhiteSpace(saved)) return saved;

        return GameRules.RandomNickname(new System.Random());
    }

    public void SignIn(string nickname, Action<UserInfo> onDone = null)
    {
        service.SignIn(nickname, user => Post(() =>
        {
            CurrentUser = user;
            if (user != null)
            {
                PlayerPrefs.SetString(NicknameKey, user.nickname);
            }
            onDone?.Invoke(user);
        }));
    }

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
        ChatReceived?.Invoke(channel, new ChatMessage { text = text, time = ServerNow });
    }
    public void ReturnToRoom() => service.ReturnToRoom();

    // 빈 글이거나 도배 제한에 걸리면 보내지 않고 false. 너무 길면 잘라서 보냄.
    public bool SendChat(ChatChannel channel, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (Time.unscaledTime - lastChatTime < GameRules.ChatCooldown) return false;

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
