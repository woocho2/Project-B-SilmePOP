using System;
using System.Collections.Generic;
using UnityEngine;

// Firebase Realtime Database로 하는 실제 네트워크. LocalNetworkService와 같은 약속(INetworkService)을 지킴.
//
// 데이터베이스 모양 (기획 [6])
//   guestCounter                 게스트 번호표. 접속할 때마다 1씩 올림
//   users/{uid}                  nickname, guestNumber, lastSeen(하트비트 시각)
//   lobbyChat/{id}               로비 채팅
//   rooms/{roomId}               roomName, hostUid, maxPlayers, state, seed, targetSum, startAt, slots/0 ~ 3
//     scores/s{자리}              점수 (판이 끝나 대기실로 돌아가면 지움)
//     finalBoards/s{자리}         최종 판 (BoardSnapshot JSON 문자열)
//     chat/{id}                   방·대전·결과 채팅 (channel로 구분)
//
// 받는 쪽: users, rooms, lobbyChat 세 곳을 실시간 수신으로 메모리에 들고 있다가(FirebaseData 나무)
//         바뀔 때마다 이전과 비교해서 이벤트를 쏨. 내가 한 변경도 서버를 거쳐 돌아올 때 이벤트가 됨.
// 끊김: REST에는 접속이 끊겼다는 알림이 없어서, 하트비트(5초)가 15초 동안 없으면 끊긴 것으로 봄.
//       방장이 끊긴 사람을 정리하고, 방장이 끊기면 남은 사람 중 자리 번호가 가장 앞인 사람이 방장이 됨.
public sealed class FirebaseNetworkService : INetworkService
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
    public event Action<int> HostChanged;

    private const string LobbyChatQuery = "orderBy=%22%24key%22&limitToLast=30";   // orderBy="$key" (최근 30개)

    private readonly FirebaseRestClient client;
    private UserInfo me;
    private int reservedGuestNumber;
    private string myColor = "#FFFFFF";
    private readonly System.Random nicknameRandom = new System.Random();   // AI 이름

    // 실시간 수신으로 들고 있는 자료
    private FirebaseRestClient.Stream usersStream, roomsStream, lobbyChatStream;
    private object usersTree, roomsTree, lobbyChatTree;
    private bool usersLoaded;
    private bool streamsBroken;
    private readonly HashSet<string> seenChat = new HashSet<string>();

    // 지금 방 (이전 값과 비교해서 이벤트를 쏘기 위해 들고 있음)
    private string currentRoomId;
    private RoomInfo currentRoom;
    private string lastRoomJson;
    private string lastScoresJson;
    private readonly HashSet<string> seenBoards = new HashSet<string>();
    private long playAtRequestedFor;   // 이 판(startAt)의 playAt을 이미 보냈으면 그 startAt
    // 내가 들어간 모습의 방이 실시간 수신으로 도착했는가. 도착 전에는 방이 없거나 내 자리가 없어도
    // 아직 소식이 늦은 것이므로 나가지 않음 (방을 만들거나 들어간 직후의 짧은 사이)
    private bool roomArrived;

    public long ServerNow => client.ServerNow;

    public FirebaseNetworkService(FirebaseRestClient client)
    {
        this.client = client;
    }

    // ── 접속 ────────────────────────────────────────────────────

    public void CheckConnection(Action<bool> onDone)
    {
        client.CheckConnection(onDone);
    }

    public void ReserveGuestNumber(Action<int> onDone)
    {
        if (reservedGuestNumber > 0)
        {
            onDone?.Invoke(reservedGuestNumber);
            return;
        }

        EnsureAuth(ok =>
        {
            if (!ok) { Fail("서버에 로그인하지 못했습니다."); onDone?.Invoke(0); return; }

            client.Transaction("guestCounter", current => (current is long number ? number : 0L) + 1, (written, value) =>
            {
                if (!written || !(value is long number)) { Fail("UID를 받지 못했습니다."); onDone?.Invoke(0); return; }

                reservedGuestNumber = (int)number;
                onDone?.Invoke(reservedGuestNumber);
            });
        });
    }

    public void SignIn(string nickname, Action<UserInfo> onDone)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            Fail("닉네임을 입력하세요.");
            onDone?.Invoke(null);
            return;
        }

        ReserveGuestNumber(number =>
        {
            if (number <= 0) { onDone?.Invoke(null); return; }

            Dictionary<string, object> user = new Dictionary<string, object>
            {
                { "nickname", nickname.Trim() },
                { "guestNumber", number },
                { "lastSeen", FirebaseData.ServerTimestamp }
            };
            client.Put("users/" + client.Uid, user, (ok, _) =>
            {
                if (!ok) { Fail("로그인하지 못했습니다."); onDone?.Invoke(null); return; }

                client.SyncServerTime($"users/{client.Uid}/lastSeen", () =>
                {
                    me = new UserInfo(client.Uid, nickname.Trim(), number);
                    reservedGuestNumber = 0;
                    StartStreams();
                    Connected?.Invoke();
                    onDone?.Invoke(me);
                });
            });
        });
    }

    public void SignOut()
    {
        if (me == null) return;

        LeaveRoom();
        client.Delete("users/" + me.uid);
        CloseStreams();
        me = null;
        usersTree = roomsTree = lobbyChatTree = null;
        usersLoaded = false;
        seenChat.Clear();
        Disconnected?.Invoke();
    }

    public void SendHeartbeat()
    {
        if (me == null) return;

        if (client.NeedsTokenRefresh)
        {
            client.RefreshToken(ok => { if (ok) StartStreams(); });
            return;
        }
        if (streamsBroken) StartStreams();

        client.Patch("users/" + me.uid, new Dictionary<string, object> { { "lastSeen", FirebaseData.ServerTimestamp } });
        PublishLobbyUsers();   // 시간이 지나 끊긴 것으로 바뀐 사람이 있을 수 있음
        CheckPresence();
        if (currentRoom != null) TryStartCountdown();   // 안 온 사람이 있어도 시간이 다 되면 시작
    }

    private void EnsureAuth(Action<bool> onDone)
    {
        if (client.IsSignedIn) onDone(true);
        else client.SignInAnonymously(onDone);
    }

    // ── 실시간 수신 ─────────────────────────────────────────────

    private void StartStreams()
    {
        CloseStreams();
        streamsBroken = false;
        usersStream = client.Listen("users", null, (e, path, data) =>
        {
            usersTree = Apply(usersTree, e, path, data);
            usersLoaded = true;
            PublishLobbyUsers();
            PublishRooms();   // 방장이 살아 있는지가 바뀌었을 수 있음
        }, OnStreamEnded);
        roomsStream = client.Listen("rooms", null, (e, path, data) =>
        {
            roomsTree = Apply(roomsTree, e, path, data);
            PublishRooms();
        }, OnStreamEnded);
        lobbyChatStream = client.Listen("lobbyChat", LobbyChatQuery, (e, path, data) =>
        {
            lobbyChatTree = Apply(lobbyChatTree, e, path, data);
            PublishNewChat(lobbyChatTree, "lobby/");
        }, OnStreamEnded);
    }

    private void CloseStreams()
    {
        usersStream?.Close();
        roomsStream?.Close();
        lobbyChatStream?.Close();
        usersStream = roomsStream = lobbyChatStream = null;
    }

    // 끊긴 수신은 다음 하트비트(5초 안)에 다시 연결함 (바로 다시 하면 오류가 날 때 끝없이 반복됨)
    private void OnStreamEnded()
    {
        if (me != null) streamsBroken = true;
    }

    private static object Apply(object tree, string eventName, string path, object data)
    {
        return eventName == "patch" ? FirebaseData.ApplyPatch(tree, path, data) : FirebaseData.ApplyPut(tree, path, data);
    }

    private bool IsAlive(string uid)
    {
        object user = FirebaseData.Get(usersTree, uid);
        if (user == null) return false;
        return ServerNow - FirebaseData.Long(user, "lastSeen") <= (long)(GameRules.DisconnectTimeout * 1000f);
    }

    private void PublishLobbyUsers()
    {
        List<UserInfo> users = new List<UserInfo>();
        if (usersTree is Dictionary<string, object> all)
        {
            foreach (KeyValuePair<string, object> pair in all)
            {
                if (IsAlive(pair.Key)) users.Add(FirebaseData.ToUser(pair.Key, pair.Value));
            }
        }
        LobbyUsersChanged?.Invoke(users);
    }

    private void PublishRooms()
    {
        List<RoomInfo> rooms = new List<RoomInfo>();
        if (roomsTree is Dictionary<string, object> all)
        {
            foreach (KeyValuePair<string, object> pair in all)
            {
                RoomInfo room = FirebaseData.ToRoom(pair.Key, pair.Value);
                if (!usersLoaded || IsAlive(room.hostUid)) rooms.Add(room);   // 방장이 끊긴 방은 목록에서 뺌
            }
        }
        RoomListChanged?.Invoke(rooms);

        if (currentRoomId != null) UpdateCurrentRoom();
    }

    // 지금 방이 이전과 달라졌으면 해당 이벤트를 쏨
    private void UpdateCurrentRoom()
    {
        object node = FirebaseData.Get(roomsTree, currentRoomId);
        RoomInfo room = node != null ? FirebaseData.ToRoom(currentRoomId, node) : null;
        if (room == null || room.FindSlot(me.uid) < 0)
        {
            // 도착한 뒤라면 방이 지워졌거나, 끊긴 것으로 처리되어 자리에서 빠진 것
            if (roomArrived) ExitRoomLocally();
            return;
        }
        roomArrived = true;

        RoomInfo previous = currentRoom;
        string roomJson = Json.Serialize(FirebaseData.FromRoom(room));
        if (roomJson != lastRoomJson)
        {
            lastRoomJson = roomJson;
            currentRoom = room;
            RoomChanged?.Invoke(room.Clone());

            if (previous != null && previous.state == RoomState.Waiting && room.state == RoomState.Playing)
            {
                MatchStarting?.Invoke(new MatchSettings(room.seed, room.targetSum, room.startAt, room.PlayerCount));
            }
            if (previous != null && previous.hostUid != room.hostUid)
            {
                HostChanged?.Invoke(room.FindSlot(room.hostUid));
            }
        }

        object scores = FirebaseData.Get(node, "scores");
        string scoresJson = Json.Serialize(scores);
        if (scoresJson != lastScoresJson)
        {
            lastScoresJson = scoresJson;
            ScoresChanged?.Invoke(FirebaseData.ToScores(scores));
        }

        PublishNewBoards(FirebaseData.Child(node, "finalBoards"));
        PublishNewChat(FirebaseData.Get(node, "chat"), $"room/{currentRoomId}/");
        TryStartCountdown();
    }

    private void PublishNewBoards(Dictionary<string, object> boards)
    {
        if (boards == null)
        {
            seenBoards.Clear();   // 대기실로 돌아가며 지워짐
            return;
        }

        foreach (KeyValuePair<string, object> pair in boards)
        {
            if (!seenBoards.Add(pair.Key) || !(pair.Value is string json)) continue;
            if (!int.TryParse(pair.Key.Substring(1), out int slot)) continue;
            FinalBoardReceived?.Invoke(slot, JsonUtility.FromJson<BoardSnapshot>(json));
        }
    }

    // 처음 보는 메시지만 시간 순서(키 순서)로 알림
    private void PublishNewChat(object chatNode, string seenPrefix)
    {
        if (!(chatNode is Dictionary<string, object> messages)) return;

        List<string> keys = new List<string>(messages.Keys);
        keys.Sort(string.CompareOrdinal);
        foreach (string key in keys)
        {
            if (!seenChat.Add(seenPrefix + key)) continue;
            ChatMessage message = FirebaseData.ToChat(messages[key], out ChatChannel channel);
            ChatReceived?.Invoke(seenPrefix == "lobby/" ? ChatChannel.Lobby : channel, message);
        }
    }

    // ── 방 ──────────────────────────────────────────────────────

    public void CreateRoom(string roomName, int maxPlayers, Action<RoomInfo> onDone)
    {
        if (me == null) { Fail("먼저 로그인하세요."); onDone?.Invoke(null); return; }
        if (maxPlayers < 1 || maxPlayers > GameRules.MaxPlayers)
        {
            Fail($"인원은 1 ~ {GameRules.MaxPlayers}명이어야 합니다.");
            onDone?.Invoke(null);
            return;
        }

        LeaveRoom();

        RoomInfo room = new RoomInfo
        {
            roomName = roomName,
            hostUid = me.uid,
            maxPlayers = maxPlayers,
            state = RoomState.Waiting,
            slots = new SlotInfo[GameRules.MaxPlayers]
        };
        for (int i = 0; i < room.slots.Length; i++) room.slots[i] = new SlotInfo { kind = SlotKind.Empty };
        room.slots[0] = MySlot();

        client.Post("rooms", FirebaseData.FromRoom(room), roomId =>
        {
            if (roomId == null) { Fail("방을 만들지 못했습니다."); onDone?.Invoke(null); return; }

            room.roomId = roomId;
            EnterRoom(room);
            onDone?.Invoke(room.Clone());
        });
    }

    public void JoinRoom(string roomId, Action<RoomInfo> onDone)
    {
        if (me == null) { Fail("먼저 로그인하세요."); onDone?.Invoke(null); return; }

        LeaveRoom();
        string reason = null;
        client.Transaction("rooms/" + roomId, current =>
        {
            if (current == null) { reason = "방을 찾을 수 없습니다."; return FirebaseRestClient.Abort; }

            Dictionary<string, object> node = (Dictionary<string, object>)FirebaseData.Normalize(current);
            RoomInfo room = FirebaseData.ToRoom(roomId, node);
            if (room.state != RoomState.Waiting) { reason = "이미 게임이 시작된 방입니다."; return FirebaseRestClient.Abort; }
            if (room.FindSlot(me.uid) >= 0) return node;   // 이미 들어가 있음

            int free = room.FindEmptySlot();
            if (free < 0) { reason = "방이 가득 찼습니다."; return FirebaseRestClient.Abort; }

            SlotsOf(node)[free.ToString()] = FirebaseData.FromSlot(MySlot());
            return node;
        }, (ok, value) =>
        {
            if (!ok) { Fail(reason ?? "방에 들어가지 못했습니다."); onDone?.Invoke(null); return; }

            RoomInfo room = FirebaseData.ToRoom(roomId, FirebaseData.Normalize(value));
            EnterRoom(room);
            onDone?.Invoke(room.Clone());
        });
    }

    // 대기 중이면 자리를 비우고, 대전 중이면 기권으로 남김. 방장이 나가면 남은 사람에게 넘기고, 사람이 없으면 방을 지움.
    public void LeaveRoom()
    {
        if (currentRoomId == null || me == null) return;

        string roomId = currentRoomId;
        string myUid = me.uid;
        long now = ServerNow;
        ExitRoomLocally();

        client.Transaction("rooms/" + roomId, current =>
        {
            if (current == null) return FirebaseRestClient.Abort;

            Dictionary<string, object> node = (Dictionary<string, object>)FirebaseData.Normalize(current);
            RoomInfo room = FirebaseData.ToRoom(roomId, node);
            int slot = room.FindSlot(myUid);
            if (slot < 0) return FirebaseRestClient.Abort;

            if (room.state == RoomState.Waiting)
            {
                SlotsOf(node)[slot.ToString()] = FirebaseData.FromSlot(new SlotInfo { kind = SlotKind.Empty });
                room.slots[slot] = new SlotInfo { kind = SlotKind.Empty };
            }
            else
            {
                MarkForfeit(node, slot, room, now);
            }

            if (room.hostUid == myUid)
            {
                string nextHost = FirstHuman(room, exceptUid: myUid);
                if (nextHost == null) return null;   // 사람이 없으면 방을 지움
                node["hostUid"] = nextHost;
            }
            return node;
        }, (ok, value) => { });
    }

    public void SetMyColor(string colorHex)
    {
        myColor = colorHex;
        PatchMySlot("slimeColor", colorHex);
    }

    public void AddAi(int slot, AiDifficulty difficulty)
    {
        if (!CanEditSlot(slot)) return;

        string newName = GameRules.RandomNickname(nicknameRandom);
        // 그 사이에 누가 그 자리에 들어왔으면 넣지 않음
        client.Transaction($"rooms/{currentRoomId}/slots/{slot}", current =>
        {
            SlotInfo existing = FirebaseData.ToSlot(current);
            if (existing.kind == SlotKind.Human)
            {
                Fail("사람이 있는 자리에는 AI를 넣을 수 없습니다.");
                return FirebaseRestClient.Abort;
            }

            // 이미 AI가 있는 자리면 이름은 그대로 두고 난이도만 바꿈
            return FirebaseData.FromSlot(new SlotInfo
            {
                kind = SlotKind.Ai,
                displayName = existing.kind == SlotKind.Ai ? existing.displayName : newName,
                difficulty = difficulty,
                slimeColor = GameRules.AiSlimeColors[slot % GameRules.AiSlimeColors.Length]
            });
        }, (ok, value) => { });
    }

    public void RemoveAi(int slot)
    {
        if (!CanEditSlot(slot)) return;

        client.Transaction($"rooms/{currentRoomId}/slots/{slot}", current =>
        {
            if (FirebaseData.ToSlot(current).kind != SlotKind.Ai) return FirebaseRestClient.Abort;
            return FirebaseData.FromSlot(new SlotInfo { kind = SlotKind.Empty });
        }, (ok, value) => { });
    }

    public void StartMatch()
    {
        if (!IsHostOf(currentRoom) || currentRoom.state != RoomState.Waiting) return;

        long startAt = ServerNow + (long)(GameRules.StartDelay * 1000f);
        MatchSettings settings = MatchSettings.CreateNew(startAt, currentRoom.PlayerCount);

        Dictionary<string, object> scores = new Dictionary<string, object>();
        for (int i = 0; i < currentRoom.slots.Length; i++)
        {
            if (currentRoom.slots[i].kind != SlotKind.Empty)
            {
                scores[FirebaseData.ScoreKey(i)] = FirebaseData.FromScore(new ScoreEntry { slot = i });
            }
        }

        // 한 번에 써서 모두가 같은 순간에 시작 정보를 받음 (MatchStarting은 수신에서 쏨)
        client.Patch("rooms/" + currentRoomId, new Dictionary<string, object>
        {
            { "seed", settings.Seed },
            { "targetSum", settings.TargetSum },
            { "startAt", settings.StartAt },
            { "playAt", 0 },
            { "state", (int)RoomState.Playing },
            { "scores", scores },
            { "loaded", null },
            { "finalBoards", null }
        });
    }

    public void ReportLoaded(int slot)
    {
        if (currentRoomId == null) return;
        client.Put($"rooms/{currentRoomId}/loaded/{FirebaseData.ScoreKey(slot)}", true);
    }

    // 방장: 사람이 모두 게임 씬에 왔거나 너무 오래 기다렸으면 3, 2, 1을 시작할 시각을 정함
    private void TryStartCountdown()
    {
        RoomInfo room = currentRoom;
        if (!IsHostOf(room) || room.state != RoomState.Playing || room.playAt > 0) return;
        if (playAtRequestedFor == room.startAt) return;   // 이미 보냄. 돌아올 때까지 기다림

        Dictionary<string, object> loaded = FirebaseData.Child(FirebaseData.Get(roomsTree, currentRoomId), "loaded");
        bool everyoneLoaded = true;
        for (int i = 0; i < room.slots.Length; i++)
        {
            if (room.slots[i].kind == SlotKind.Human && !FirebaseData.Bool(loaded, FirebaseData.ScoreKey(i))) everyoneLoaded = false;
        }
        bool timedOut = ServerNow > room.startAt + (long)(GameRules.LoadTimeout * 1000f);
        if (!everyoneLoaded && !timedOut) return;

        playAtRequestedFor = room.startAt;
        client.Patch("rooms/" + currentRoomId, new Dictionary<string, object>
        {
            { "playAt", ServerNow + (long)(GameRules.CountdownLead * 1000f) }
        });
    }

    public void EndMatch()
    {
        if (!IsHostOf(currentRoom) || currentRoom.state != RoomState.Playing) return;
        client.Patch("rooms/" + currentRoomId, new Dictionary<string, object> { { "state", (int)RoomState.Result } });
    }

    public void ReturnToRoom()
    {
        if (currentRoom == null) return;

        client.Patch("rooms/" + currentRoomId, new Dictionary<string, object>
        {
            { "state", (int)RoomState.Waiting },
            { "playAt", 0 },
            { "scores", null },
            { "loaded", null },
            { "finalBoards", null }
        });
    }

    // ── 채팅과 점수 ─────────────────────────────────────────────

    public void SendChat(ChatChannel channel, string text)
    {
        if (me == null) return;

        Dictionary<string, object> message = FirebaseData.FromChat(me.uid, me.DisplayName, text, channel);
        if (channel == ChatChannel.Lobby) client.Post("lobbyChat", message);
        else if (currentRoomId != null) client.Post($"rooms/{currentRoomId}/chat", message);
    }

    public void ReportScore(int slot, int score, float lastScoreTime)
    {
        if (currentRoomId == null) return;

        client.Patch($"rooms/{currentRoomId}/scores/{FirebaseData.ScoreKey(slot)}", new Dictionary<string, object>
        {
            { "slot", slot },
            { "score", score },
            { "lastScoreTime", lastScoreTime }
        });
    }

    public void ReportForfeit(int slot, float forfeitTime)
    {
        if (currentRoomId == null) return;

        client.Patch($"rooms/{currentRoomId}/scores/{FirebaseData.ScoreKey(slot)}", new Dictionary<string, object>
        {
            { "slot", slot },
            { "forfeited", true },
            { "forfeitTime", forfeitTime }
        });
    }

    public void UploadFinalBoard(int slot, BoardSnapshot snapshot)
    {
        if (currentRoomId == null) return;
        client.Put($"rooms/{currentRoomId}/finalBoards/{FirebaseData.ScoreKey(slot)}", JsonUtility.ToJson(snapshot));
    }

    // ── 끊김 처리 (하트비트마다) ─────────────────────────────────

    private void CheckPresence()
    {
        if (currentRoom == null || !usersLoaded) return;

        RoomInfo room = currentRoom;
        if (room.hostUid != me.uid && !IsAlive(room.hostUid))
        {
            // 방장이 끊김: 살아 있는 사람 중 자리 번호가 가장 앞인 사람이 방장을 맡음
            if (FirstHuman(room, exceptUid: room.hostUid) == me.uid)
            {
                client.Patch("rooms/" + currentRoomId, new Dictionary<string, object> { { "hostUid", me.uid } });
            }
            return;
        }
        if (room.hostUid != me.uid) return;

        // 방장: 끊긴 사람을 정리 (대기 중이면 자리를 비우고, 대전 중이면 기권)
        Dictionary<string, object> changes = new Dictionary<string, object>();
        for (int i = 0; i < room.slots.Length; i++)
        {
            SlotInfo slot = room.slots[i];
            if (slot.kind != SlotKind.Human || slot.uid == me.uid || IsAlive(slot.uid)) continue;

            if (room.state == RoomState.Waiting)
            {
                changes[$"slots/{i}"] = FirebaseData.FromSlot(new SlotInfo { kind = SlotKind.Empty });
            }
            else
            {
                string key = $"scores/{FirebaseData.ScoreKey(i)}";
                if (FirebaseData.Bool(FirebaseData.Get(FirebaseData.Get(FirebaseData.Get(roomsTree, currentRoomId), "scores"), FirebaseData.ScoreKey(i)), "forfeited")) continue;
                changes[key + "/forfeited"] = true;
                changes[key + "/forfeitTime"] = ElapsedSeconds(room, ServerNow);
            }
        }
        if (changes.Count > 0) client.Patch("rooms/" + currentRoomId, changes);
    }

    // ── 도움 함수 ───────────────────────────────────────────────

    private void EnterRoom(RoomInfo room)
    {
        currentRoomId = room.roomId;
        currentRoom = room;
        roomArrived = false;
        lastRoomJson = Json.Serialize(FirebaseData.FromRoom(room));
        lastScoresJson = null;
        seenBoards.Clear();
        RoomChanged?.Invoke(room.Clone());
        UpdateCurrentRoom();   // 이미 받은 자료가 있으면 점수·채팅까지 맞춤
    }

    private void ExitRoomLocally()
    {
        if (currentRoomId == null) return;

        currentRoomId = null;
        currentRoom = null;
        roomArrived = false;
        lastRoomJson = null;
        lastScoresJson = null;
        seenBoards.Clear();
        RoomChanged?.Invoke(null);
        ScoresChanged?.Invoke(new ScoreEntry[0]);
    }

    private SlotInfo MySlot()
    {
        return new SlotInfo { kind = SlotKind.Human, uid = me.uid, displayName = me.DisplayName, slimeColor = myColor };
    }

    private void PatchMySlot(string field, object value)
    {
        if (currentRoom == null || me == null) return;

        int slot = currentRoom.FindSlot(me.uid);
        if (slot < 0) return;
        client.Patch($"rooms/{currentRoomId}/slots/{slot}", new Dictionary<string, object> { { field, value } });
    }

    private bool IsHostOf(RoomInfo room)
    {
        return room != null && me != null && room.hostUid == me.uid;
    }

    private bool CanEditSlot(int slot)
    {
        return IsHostOf(currentRoom) && currentRoom.state == RoomState.Waiting && slot >= 0 && slot < currentRoom.maxPlayers;
    }

    // 살아 있는 사람 중 자리 번호가 가장 앞인 사람 (exceptUid는 뺌)
    private string FirstHuman(RoomInfo room, string exceptUid)
    {
        foreach (SlotInfo slot in room.slots)
        {
            if (slot.kind != SlotKind.Human || slot.uid == exceptUid) continue;
            if (!usersLoaded || IsAlive(slot.uid)) return slot.uid;
        }
        return null;
    }

    private static Dictionary<string, object> SlotsOf(Dictionary<string, object> roomNode)
    {
        if (!(FirebaseData.Get(roomNode, "slots") is Dictionary<string, object> slots))
        {
            slots = new Dictionary<string, object>();
            roomNode["slots"] = slots;
        }
        return slots;
    }

    private static void MarkForfeit(Dictionary<string, object> roomNode, int slot, RoomInfo room, long now)
    {
        if (!(FirebaseData.Get(FirebaseData.Get(roomNode, "scores"), FirebaseData.ScoreKey(slot)) is Dictionary<string, object> entry)) return;
        if (FirebaseData.Bool(entry, "forfeited")) return;

        entry["forfeited"] = true;
        entry["forfeitTime"] = ElapsedSeconds(room, now);
    }

    // 경기 시작(startAt + 카운트다운)부터 지난 시간. 기권 순서를 정할 때 씀.
    private static float ElapsedSeconds(RoomInfo room, long now)
    {
        return Mathf.Max(0f, (now - room.startAt) / 1000f - GameRules.CountdownFrom);
    }

    private void Fail(string message)
    {
        ErrorOccurred?.Invoke(message);
    }
}
