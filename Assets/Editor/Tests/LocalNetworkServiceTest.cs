using System.Collections.Generic;
using NUnit.Framework;

public class LocalNetworkServiceTest
{
    private static LocalNetworkService SignedIn()
    {
        LocalNetworkService service = new LocalNetworkService();
        service.SignUpWithEmail("slime@test.com", "123456", "슬라임", null);
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
    public void SignUp_RejectsBlankNickname()
    {
        LocalNetworkService service = new LocalNetworkService();
        string error = null;
        UserInfo user = new UserInfo();
        service.ErrorOccurred += message => error = message;

        service.SignUpWithEmail("slime@test.com", "123456", "   ", result => user = result);

        Assert.IsNull(user);
        Assert.AreEqual("닉네임을 입력하세요.", error);
    }

    [TestCase("말랑슬라임", null)]
    [TestCase("전설의청록색슬라임", null)]
    [TestCase("", "닉네임을 입력하세요.")]
    [TestCase("a", "닉네임은 2~12자로 입력하세요.")]
    [TestCase("1234567890123", "닉네임은 2~12자로 입력하세요.")]
    [TestCase("말랑 슬라임", "닉네임에 공백이나 #은 쓸 수 없습니다.")]
    [TestCase("슬라임#1", "닉네임에 공백이나 #은 쓸 수 없습니다.")]
    public void CheckNickname_GivesReason(string nickname, string reason)
    {
        Assert.AreEqual(reason, GameRules.CheckNickname(nickname));
    }

    [Test]
    public void DisplayName_ShowsAccountNumberInFourDigits()
    {
        Assert.AreEqual("슬라임#0012", new UserInfo("uid-1", "슬라임", 12).DisplayName);
    }

    [Test]
    public void SignUp_NumbersAccountsInOrder()
    {
        LocalNetworkService service = new LocalNetworkService();
        UserInfo first = null;
        UserInfo second = null;

        service.SignUpWithEmail("one@test.com", "123456", "하나", result => first = result);
        service.SignOut();
        service.SignUpWithEmail("two@test.com", "123456", "둘", result => second = result);

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
    public void Email_SignUpThenLoginAgain_KeepsNicknameAndNumber()
    {
        LocalNetworkService service = new LocalNetworkService();
        UserInfo first = null;
        UserInfo again = null;

        service.SignUpWithEmail("slime@test.com", "123456", "말랑슬라임", result => first = result);
        service.SignOut();
        service.SignInWithEmail("slime@test.com", "123456", result => again = result);

        Assert.IsNotNull(first);
        Assert.AreEqual(first.uid, again.uid);
        Assert.AreEqual(first.DisplayName, again.DisplayName);
    }

    [TestCase("slime@test.com", "654321", "이메일 또는 비밀번호가 맞지 않습니다.")]
    [TestCase("nobody@test.com", "123456", "이메일 또는 비밀번호가 맞지 않습니다.")]
    [TestCase("not-an-email", "123456", "이메일 형식이 올바르지 않습니다.")]
    [TestCase("slime@test.com", "", "비밀번호를 입력하세요.")]
    public void Email_LoginFailure_GivesReason(string email, string password, string reason)
    {
        LocalNetworkService service = new LocalNetworkService();
        service.SignUpWithEmail("slime@test.com", "123456", "말랑슬라임", null);
        service.SignOut();
        string error = null;
        UserInfo user = new UserInfo();
        service.ErrorOccurred += message => error = message;

        service.SignInWithEmail(email, password, result => user = result);

        Assert.IsNull(user);
        Assert.AreEqual(reason, error);
    }

    [Test]
    public void Email_SignUpRejectsDuplicateAndShortPassword()
    {
        LocalNetworkService service = new LocalNetworkService();
        List<string> errors = new List<string>();
        service.ErrorOccurred += errors.Add;
        service.SignUpWithEmail("slime@test.com", "123456", "하나", null);
        service.SignOut();

        service.SignUpWithEmail("slime@test.com", "123456", "둘", null);
        service.SignUpWithEmail("new@test.com", "123", "셋", null);

        CollectionAssert.AreEqual(new[] { "이미 가입된 이메일입니다.", "비밀번호는 6자 이상이어야 합니다." }, errors);
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
