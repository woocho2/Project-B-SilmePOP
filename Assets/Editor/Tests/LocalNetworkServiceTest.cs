using System.Collections.Generic;
using NUnit.Framework;

public class LocalNetworkServiceTest
{
    private static LocalNetworkService SignedIn()
    {
        LocalNetworkService service = new LocalNetworkService();
        service.SignIn("슬라임", null);
        return service;
    }

    [Test]
    public void CheckConnection_WorksBeforeSignIn()
    {
        bool? connected = null;

        new LocalNetworkService().CheckConnection(result => connected = result);

        Assert.AreEqual(true, connected);
    }

    [Test]
    public void SignIn_RejectsBlankNickname()
    {
        LocalNetworkService service = new LocalNetworkService();
        string error = null;
        UserInfo user = new UserInfo();
        service.ErrorOccurred += message => error = message;

        service.SignIn("   ", result => user = result);

        Assert.IsNull(user);
        Assert.IsNotNull(error);
    }

    [Test]
    public void DisplayName_ShowsGuestNumberInFourDigits()
    {
        Assert.AreEqual("슬라임#0012", new UserInfo("uid-1", "슬라임", 12).DisplayName);
    }

    [Test]
    public void SignIn_UsesReservedNumber_EvenIfReservedTwice()
    {
        LocalNetworkService service = new LocalNetworkService();
        int first = 0;
        int second = 0;
        UserInfo user = null;

        service.ReserveGuestNumber(number => first = number);
        service.ReserveGuestNumber(number => second = number);
        service.SignIn("슬라임", result => user = result);

        Assert.AreEqual(first, second);
        Assert.AreEqual(first, user.guestNumber);
    }

    [Test]
    public void NextGuest_GetsNextNumber()
    {
        LocalNetworkService service = new LocalNetworkService();
        UserInfo first = null;
        UserInfo second = null;

        service.SignIn("하나", result => first = result);
        service.SignOut();
        service.SignIn("둘", result => second = result);

        Assert.AreEqual(1, first.guestNumber);
        Assert.AreEqual(2, second.guestNumber);
    }

    [Test]
    public void CreateRoom_PutsMeInFirstSlotAsHost()
    {
        LocalNetworkService service = SignedIn();
        RoomInfo room = null;

        service.CreateRoom("방", GameRules.MaxPlayers, result => room = result);

        Assert.AreEqual(SlotKind.Human, room.slots[0].kind);
        Assert.AreEqual(room.hostUid, room.slots[0].uid);
        Assert.AreEqual(GameRules.MaxPlayers, room.slots.Length);
        Assert.AreEqual(1, room.PlayerCount);
    }

    [Test]
    public void CreateRoom_ClosesSlotsBeyondMaxPlayers()
    {
        LocalNetworkService service = SignedIn();
        RoomInfo room = null;
        service.RoomChanged += result => room = result;
        service.CreateRoom("방", 2, null);

        service.AddAi(1, AiDifficulty.Easy);
        service.AddAi(2, AiDifficulty.Easy);

        Assert.AreEqual(2, room.maxPlayers);
        Assert.AreEqual(SlotKind.Ai, room.slots[1].kind);
        Assert.AreEqual(SlotKind.Empty, room.slots[2].kind);
    }

    [Test]
    public void SlimeColor_MyColorFollowsSetMyColor_AiGetsSlotColor()
    {
        LocalNetworkService service = SignedIn();
        RoomInfo room = null;
        service.RoomChanged += result => room = result;
        service.SetMyColor("#B3270B");
        service.CreateRoom("방", GameRules.MaxPlayers, null);
        service.AddAi(2, AiDifficulty.Hell);

        Assert.AreEqual("#B3270B", room.slots[0].slimeColor);
        Assert.AreEqual(GameRules.AiSlimeColors[2], room.slots[2].slimeColor);

        service.SetMyColor("#257BD6");
        Assert.AreEqual("#257BD6", room.slots[0].slimeColor);
    }

    [Test]
    public void AddAi_GetsRandomNickname_KeptWhenDifficultyChanges()
    {
        LocalNetworkService service = SignedIn();
        RoomInfo room = null;
        service.RoomChanged += result => room = result;
        service.CreateRoom("방", GameRules.MaxPlayers, null);

        service.AddAi(1, AiDifficulty.Easy);
        string name = room.slots[1].displayName;
        service.AddAi(1, AiDifficulty.Hell);

        StringAssert.EndsWith("슬라임", name);
        Assert.AreEqual(name, room.slots[1].displayName);
        Assert.AreEqual(AiDifficulty.Hell, room.slots[1].difficulty);
    }

    [Test]
    public void CreateRoom_RejectsInvalidMaxPlayers()
    {
        LocalNetworkService service = SignedIn();
        RoomInfo room = new RoomInfo();

        service.CreateRoom("방", GameRules.MaxPlayers + 1, result => room = result);

        Assert.IsNull(room);
    }

    [Test]
    public void AddAi_RefusesHumanSlot_AndChangesDifficultyOnAiSlot()
    {
        LocalNetworkService service = SignedIn();
        RoomInfo room = null;
        string error = null;
        service.RoomChanged += result => room = result;
        service.ErrorOccurred += message => error = message;
        service.CreateRoom("방", GameRules.MaxPlayers, null);

        service.AddAi(0, AiDifficulty.Easy);
        Assert.IsNotNull(error);
        Assert.AreEqual(SlotKind.Human, room.slots[0].kind);

        service.AddAi(1, AiDifficulty.Easy);
        service.AddAi(1, AiDifficulty.Hard);
        Assert.AreEqual(SlotKind.Ai, room.slots[1].kind);
        Assert.AreEqual(AiDifficulty.Hard, room.slots[1].difficulty);

        service.RemoveAi(1);
        Assert.AreEqual(SlotKind.Empty, room.slots[1].kind);
    }

    [Test]
    public void StartMatch_WritesSettingsToRoom_AndGivesEveryPlayerAScore()
    {
        LocalNetworkService service = SignedIn();
        RoomInfo room = null;
        MatchSettings settings = null;
        IReadOnlyList<ScoreEntry> scores = null;
        service.RoomChanged += result => room = result;
        service.MatchStarting += result => settings = result;
        service.ScoresChanged += result => scores = result;
        service.CreateRoom("방", GameRules.MaxPlayers, null);
        service.AddAi(2, AiDifficulty.Normal);

        long before = service.ServerNow;
        service.StartMatch();

        Assert.AreEqual(RoomState.Playing, room.state);
        Assert.AreEqual(settings.Seed, room.seed);
        Assert.AreEqual(settings.TargetSum, room.targetSum);
        Assert.That(settings.TargetSum, Is.InRange(GameRules.MinTargetSum, GameRules.MaxTargetSum));
        Assert.GreaterOrEqual(settings.StartAt, before + (long)(GameRules.StartDelay * 1000f));
        Assert.AreEqual(2, settings.SlotCount);
        Assert.AreEqual(2, scores.Count);
    }

    [Test]
    public void ReportScore_ReachesScoresChanged_AndReturnToRoomClearsIt()
    {
        LocalNetworkService service = SignedIn();
        IReadOnlyList<ScoreEntry> scores = null;
        RoomInfo room = null;
        service.ScoresChanged += result => scores = result;
        service.RoomChanged += result => room = result;
        service.CreateRoom("방", GameRules.MaxPlayers, null);
        service.StartMatch();

        service.ReportScore(0, 42, 12.5f);
        Assert.AreEqual(42, scores[0].score);
        Assert.AreEqual(12.5f, scores[0].lastScoreTime);

        service.ReturnToRoom();
        Assert.AreEqual(0, scores.Count);
        Assert.AreEqual(RoomState.Waiting, room.state);
    }

    [Test]
    public void RoomEvent_SendsCopy_NotLiveState()
    {
        LocalNetworkService service = SignedIn();
        RoomInfo first = null;
        service.RoomChanged += result => { if (first == null) first = result; };
        service.CreateRoom("방", GameRules.MaxPlayers, null);

        service.AddAi(1, AiDifficulty.Easy);

        Assert.AreEqual(SlotKind.Empty, first.slots[1].kind);
    }

    [Test]
    public void Chat_EchoesBackWithMyName()
    {
        LocalNetworkService service = SignedIn();
        ChatMessage received = null;
        ChatChannel receivedChannel = ChatChannel.Lobby;
        service.ChatReceived += (channel, message) => { receivedChannel = channel; received = message; };

        service.SendChat(ChatChannel.Match, "gg");

        Assert.AreEqual(ChatChannel.Match, receivedChannel);
        Assert.AreEqual("gg", received.text);
        StringAssert.StartsWith("슬라임#", received.displayName);
    }

    [Test]
    public void LeaveRoom_SendsNullRoom()
    {
        LocalNetworkService service = SignedIn();
        RoomInfo room = new RoomInfo();
        service.RoomChanged += result => room = result;
        service.CreateRoom("방", GameRules.MaxPlayers, null);

        service.LeaveRoom();

        Assert.IsNull(room);
    }

    [Test]
    public void BoardSnapshot_RecordsEmptyAndMergedCells()
    {
        BoardModel board = new BoardModel();
        board.AddSlime(3, SlimeType.Basic, GameRules.ToIndex(0, 0));
        board.AddSlime(4, SlimeType.Angel, GameRules.ToIndex(1, 1));
        BoardSlime merged = board.Merge(GameRules.ToIndex(0, 0), out _);
        board.AddSlime(9, SlimeType.King, GameRules.ToIndex(5, 5));

        BoardSnapshot snapshot = BoardSnapshot.FromBoard(board, 3, "Spring");

        Assert.AreEqual(merged.Number, snapshot.numbers[GameRules.ToIndex(1, 0)]);
        Assert.AreEqual(merged.Anchor, snapshot.anchors[GameRules.ToIndex(1, 1)]);
        Assert.AreEqual((int)SlimeType.King, snapshot.types[GameRules.ToIndex(5, 5)]);
        Assert.AreEqual(0, snapshot.numbers[GameRules.ToIndex(9, 9)]);
        Assert.AreEqual(-1, snapshot.anchors[GameRules.ToIndex(9, 9)]);
        Assert.AreEqual(3, snapshot.colorIndex);
        Assert.AreEqual("Spring", snapshot.themeName);
    }
}
