using System;
using System.Collections.Generic;

// 서버 없이 메모리로 흉내 내는 구현. 접속자는 나 한 명, 방은 내가 만든 1개뿐이고,
// 요청을 받으면 같은 PC의 이벤트로 돌려줌. 서버 없이 로비 → 대기실 → 대전을 끝까지 만들어 보기 위한 것.
public sealed class LocalNetworkService : INetworkService
{
    public event Action Connected;
    public event Action Disconnected;
    public event Action<string> ErrorOccurred;
    public event Action<IReadOnlyList<UserInfo>> LobbyUsersChanged;
    public event Action<IReadOnlyList<RoomInfo>> RoomListChanged;
    public event Action<RoomInfo> RoomChanged;
    public event Action<ChatChannel, ChatMessage> ChatReceived;
    public event Action<MatchSettings> MatchStarting;
    public event Action<IReadOnlyList<ScoreEntry>> ScoresChanged;
    public event Action<int, BoardSnapshot> FinalBoardReceived;
#pragma warning disable 0067 // 나 혼자라 방장이 바뀔 일이 없음
    public event Action<int> HostChanged;
#pragma warning restore 0067

    private readonly List<ScoreEntry> scores = new List<ScoreEntry>();
    private UserInfo me;
    private RoomInfo room;
    private int lastGuestNumber;
    private int reservedGuestNumber;
    private string myColor = "#FFFFFF";
    private readonly Random nicknameRandom = new Random();   // AI 이름

    public long ServerNow => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public void CheckConnection(Action<bool> onDone)
    {
        onDone?.Invoke(true);   // 서버가 없으니 항상 연결됨
    }

    public void ReserveGuestNumber(Action<int> onDone)
    {
        if (reservedGuestNumber == 0) reservedGuestNumber = ++lastGuestNumber;
        onDone?.Invoke(reservedGuestNumber);
    }

    public void SignIn(string nickname, Action<UserInfo> onDone)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            ErrorOccurred?.Invoke("닉네임을 입력하세요.");
            onDone?.Invoke(null);
            return;
        }

        ReserveGuestNumber(null);
        me = new UserInfo(Guid.NewGuid().ToString("N"), nickname.Trim(), reservedGuestNumber);
        reservedGuestNumber = 0;
        Connected?.Invoke();
        onDone?.Invoke(me);
        LobbyUsersChanged?.Invoke(new[] { me });
        RoomListChanged?.Invoke(new RoomInfo[0]);
    }

    public void SignOut()
    {
        if (room != null) LeaveRoom();
        me = null;
        Disconnected?.Invoke();
    }

    public void CreateRoom(string roomName, int maxPlayers, Action<RoomInfo> onDone)
    {
        if (me == null)
        {
            ErrorOccurred?.Invoke("먼저 로그인하세요.");
            onDone?.Invoke(null);
            return;
        }
        if (maxPlayers < 1 || maxPlayers > GameRules.MaxPlayers)
        {
            ErrorOccurred?.Invoke($"인원은 1 ~ {GameRules.MaxPlayers}명이어야 합니다.");
            onDone?.Invoke(null);
            return;
        }

        if (room != null) LeaveRoom();

        room = new RoomInfo
        {
            roomId = Guid.NewGuid().ToString("N"),
            roomName = roomName,
            hostUid = me.uid,
            maxPlayers = maxPlayers,
            state = RoomState.Waiting,
            slots = new SlotInfo[GameRules.MaxPlayers]
        };
        for (int i = 0; i < room.slots.Length; i++)
        {
            room.slots[i] = new SlotInfo { kind = SlotKind.Empty };
        }
        room.slots[0] = new SlotInfo { kind = SlotKind.Human, uid = me.uid, displayName = me.DisplayName, slimeColor = myColor };

        PublishRoom();
        onDone?.Invoke(room.Clone());
    }

    public void JoinRoom(string roomId, Action<RoomInfo> onDone)
    {
        if (room == null || room.roomId != roomId)
        {
            ErrorOccurred?.Invoke("방을 찾을 수 없습니다.");
            onDone?.Invoke(null);
            return;
        }

        onDone?.Invoke(room.Clone());
    }

    public void LeaveRoom()
    {
        if (room == null) return;

        room = null;
        scores.Clear();
        RoomChanged?.Invoke(null);
        RoomListChanged?.Invoke(new RoomInfo[0]);
    }

    public void SetMyColor(string colorHex)
    {
        myColor = colorHex;
        if (room == null || me == null) return;

        int slot = room.FindSlot(me.uid);
        if (slot < 0) return;

        room.slots[slot].slimeColor = colorHex;
        PublishRoom();
    }

    public void AddAi(int slot, AiDifficulty difficulty)
    {
        if (!CanEditSlot(slot)) return;

        if (room.slots[slot].kind == SlotKind.Human)
        {
            ErrorOccurred?.Invoke("사람이 있는 자리에는 AI를 넣을 수 없습니다.");
            return;
        }

        // 이미 AI가 있는 자리면 이름은 그대로 두고 난이도만 바꿈
        string aiName = room.slots[slot].kind == SlotKind.Ai ? room.slots[slot].displayName : GameRules.RandomNickname(nicknameRandom);
        room.slots[slot] = new SlotInfo
        {
            kind = SlotKind.Ai,
            displayName = aiName,
            difficulty = difficulty,
            slimeColor = GameRules.AiSlimeColors[slot % GameRules.AiSlimeColors.Length]
        };
        PublishRoom();
    }

    public void RemoveAi(int slot)
    {
        if (!CanEditSlot(slot) || room.slots[slot].kind != SlotKind.Ai) return;

        room.slots[slot] = new SlotInfo { kind = SlotKind.Empty };
        PublishRoom();
    }

    public void StartMatch()
    {
        if (room == null || room.state != RoomState.Waiting) return;

        long startAt = ServerNow + (long)(GameRules.StartDelay * 1000f);
        MatchSettings settings = MatchSettings.CreateNew(startAt, room.PlayerCount);
        room.seed = settings.Seed;
        room.targetSum = settings.TargetSum;
        room.startAt = settings.StartAt;
        room.playAt = 0;
        room.state = RoomState.Playing;

        scores.Clear();
        for (int i = 0; i < room.slots.Length; i++)
        {
            if (room.slots[i].kind != SlotKind.Empty) scores.Add(new ScoreEntry { slot = i });
        }

        PublishRoom();
        MatchStarting?.Invoke(settings);
        PublishScores();
    }

    // 사람은 나 하나뿐이라 내가 오면 모두 온 것
    public void ReportLoaded(int slot)
    {
        if (room == null || room.state != RoomState.Playing || room.playAt > 0) return;

        room.playAt = ServerNow + (long)(GameRules.CountdownLead * 1000f);
        PublishRoom();
    }

    public void EndMatch()
    {
        if (room == null || room.state != RoomState.Playing) return;

        room.state = RoomState.Result;
        PublishRoom();
    }

    public void SendChat(ChatChannel channel, string text)
    {
        if (me == null) return;

        ChatReceived?.Invoke(channel, new ChatMessage
        {
            uid = me.uid,
            displayName = me.DisplayName,
            text = text,
            time = ServerNow
        });
    }

    public void ReportScore(int slot, int score, float lastScoreTime)
    {
        ScoreEntry entry = FindScore(slot);
        if (entry == null) return;

        entry.score = score;
        entry.lastScoreTime = lastScoreTime;
        PublishScores();
    }

    public void ReportForfeit(int slot, float forfeitTime)
    {
        ScoreEntry entry = FindScore(slot);
        if (entry == null) return;

        entry.forfeited = true;
        entry.forfeitTime = forfeitTime;
        PublishScores();
    }

    public void UploadFinalBoard(int slot, BoardSnapshot snapshot)
    {
        FinalBoardReceived?.Invoke(slot, snapshot);
    }

    public void ReturnToRoom()
    {
        if (room == null) return;

        room.state = RoomState.Waiting;
        room.playAt = 0;
        scores.Clear();
        PublishRoom();
        PublishScores();
    }

    public void SendHeartbeat()
    {
        // 접속이 끊길 일이 없음
    }

    private bool CanEditSlot(int slot)
    {
        return room != null && room.state == RoomState.Waiting && slot >= 0 && slot < room.maxPlayers;
    }

    private ScoreEntry FindScore(int slot)
    {
        foreach (ScoreEntry entry in scores)
        {
            if (entry.slot == slot) return entry;
        }
        return null;
    }

    // 받는 쪽이 들고 있는 값이 나중에 저절로 바뀌지 않도록 복사본을 보냄 (실제 서버와 같은 동작).
    private void PublishRoom()
    {
        RoomChanged?.Invoke(room.Clone());
        RoomListChanged?.Invoke(new[] { room.Clone() });
    }

    private void PublishScores()
    {
        ScoreEntry[] copy = new ScoreEntry[scores.Count];
        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = scores[i].Clone();
        }
        ScoresChanged?.Invoke(copy);
    }
}
