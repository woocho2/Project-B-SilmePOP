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

    public bool IsConnected => CurrentUser != null;
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
    }

    private INetworkService CreateService()
    {
        if (backend == NetworkBackend.Firebase)
        {
            Debug.LogWarning("Firebase 구현은 아직 없어 Local로 동작합니다.", this);
        }
        return new LocalNetworkService();
    }

    private void SubscribeService()
    {
        service.Connected += () => Post(() => Connected?.Invoke());
        service.Disconnected += () => Post(HandleDisconnected);
        service.ErrorOccurred += message => Post(() => ErrorOccurred?.Invoke(message));
        service.LobbyUsersChanged += users => Post(() => LobbyUsersChanged?.Invoke(users));
        service.RoomListChanged += rooms => Post(() => RoomListChanged?.Invoke(rooms));
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

        string personality = GameRules.NicknamePersonalities[UnityEngine.Random.Range(0, GameRules.NicknamePersonalities.Length)];
        string color = GameRules.NicknameColors[UnityEngine.Random.Range(0, GameRules.NicknameColors.Length)];
        return $"{personality}{color}슬라임";
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

    public void CreateRoom(string roomName, Action<RoomInfo> onDone = null)
    {
        service.CreateRoom(roomName, room => Post(() => onDone?.Invoke(room)));
    }

    public void JoinRoom(string roomId, Action<RoomInfo> onDone = null)
    {
        service.JoinRoom(roomId, room => Post(() => onDone?.Invoke(room)));
    }

    public void LeaveRoom() => service.LeaveRoom();
    public void SetReady(bool ready) => service.SetReady(ready);
    public void AddAi(int slot, AiDifficulty difficulty) => service.AddAi(slot, difficulty);
    public void RemoveAi(int slot) => service.RemoveAi(slot);
    public void StartMatch() => service.StartMatch();
    public void EndMatch() => service.EndMatch();
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
