using System;
using System.Collections.Generic;

// Firebase 자료 다루기 (순수 C#이라 Unity 밖에서도 테스트됨).
// 1) 나무: 실시간 수신은 바뀐 조각(path + data)만 보내므로, 그 조각을 끼워 넣어 전체 모습을 메모리에 들고 있음.
// 2) 변환: 나무의 노드 ↔ RoomInfo, SlotInfo 같은 게임 자료형.
// 나무의 모양: 객체는 Dictionary<string, object>. Firebase는 키가 0, 1, 2…인 객체를 배열로 돌려주기도 해서
//             배열은 모두 "0", "1"… 키의 객체로 바꿔 한 모양으로 맞춤 (Normalize).
public static class FirebaseData
{
    // 쓰면 Firebase 서버가 자기 시각(밀리초)으로 바꿔 저장함
    public static Dictionary<string, object> ServerTimestamp => new Dictionary<string, object> { { ".sv", "timestamp" } };

    public static string ScoreKey(int slot) => "s" + slot;

    // ── 나무 ────────────────────────────────────────────────────

    public static object Normalize(object value)
    {
        if (value is Dictionary<string, object> dictionary)
        {
            Dictionary<string, object> copy = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                if (pair.Value != null) copy[pair.Key] = Normalize(pair.Value);
            }
            return copy;
        }
        if (value is List<object> list)
        {
            Dictionary<string, object> copy = new Dictionary<string, object>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null) copy[i.ToString()] = Normalize(list[i]);
            }
            return copy;
        }
        return value;
    }

    // 실시간 수신의 put: path 자리를 data로 바꿈 (null이면 지움). 바뀐 뒤의 뿌리를 돌려줌.
    public static object ApplyPut(object root, string path, object data)
    {
        string[] keys = SplitPath(path);
        data = Normalize(data);
        if (keys.Length == 0) return data;

        Dictionary<string, object> node = root as Dictionary<string, object>;
        if (node == null)
        {
            if (data == null) return root;
            node = new Dictionary<string, object>();
            root = node;
        }

        for (int i = 0; i < keys.Length - 1; i++)
        {
            Dictionary<string, object> next = null;
            if (node.TryGetValue(keys[i], out object child)) next = child as Dictionary<string, object>;
            if (next == null)
            {
                if (data == null) return root;   // 지울 것이 이미 없음
                next = new Dictionary<string, object>();
                node[keys[i]] = next;
            }
            node = next;
        }

        string last = keys[keys.Length - 1];
        if (data == null) node.Remove(last);
        else node[last] = data;
        return root;
    }

    // 실시간 수신의 patch: path 아래의 여러 자식을 한꺼번에 바꿈.
    public static object ApplyPatch(object root, string path, object data)
    {
        if (!(data is Dictionary<string, object> children)) return root;

        string prefix = path.TrimEnd('/');
        foreach (KeyValuePair<string, object> pair in children)
        {
            root = ApplyPut(root, prefix + "/" + pair.Key, pair.Value);
        }
        return root;
    }

    public static string[] SplitPath(string path)
    {
        return (path ?? "").Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
    }

    // ── 노드 읽기 (없거나 모양이 다르면 기본값) ─────────────────

    public static Dictionary<string, object> Child(object node, string key)
    {
        return Get(node, key) as Dictionary<string, object>;
    }

    public static object Get(object node, string key)
    {
        return node is Dictionary<string, object> dictionary && dictionary.TryGetValue(key, out object value) ? value : null;
    }

    public static string Str(object node, string key, string fallback = "")
    {
        return Get(node, key) is string text ? text : fallback;
    }

    public static long Long(object node, string key, long fallback = 0)
    {
        object value = Get(node, key);
        if (value is long whole) return whole;
        if (value is double fraction) return (long)fraction;
        return fallback;
    }

    public static int Int(object node, string key, int fallback = 0) => (int)Long(node, key, fallback);

    public static float Float(object node, string key, float fallback = 0f)
    {
        object value = Get(node, key);
        if (value is double fraction) return (float)fraction;
        if (value is long whole) return whole;
        return fallback;
    }

    public static bool Bool(object node, string key, bool fallback = false)
    {
        return Get(node, key) is bool flag ? flag : fallback;
    }

    // ── 게임 자료형 ↔ 노드 ──────────────────────────────────────

    public static UserInfo ToUser(string uid, object node)
    {
        return new UserInfo(uid, Str(node, "nickname"), Int(node, "guestNumber"));
    }

    public static Dictionary<string, object> FromSlot(SlotInfo slot)
    {
        return new Dictionary<string, object>
        {
            { "kind", (int)slot.kind },
            { "uid", slot.uid ?? "" },
            { "displayName", slot.displayName ?? "" },
            { "difficulty", (int)slot.difficulty },
            { "slimeColor", slot.slimeColor ?? "" }
        };
    }

    public static SlotInfo ToSlot(object node)
    {
        if (node == null) return new SlotInfo { kind = SlotKind.Empty };

        return new SlotInfo
        {
            kind = (SlotKind)Int(node, "kind"),
            uid = Str(node, "uid"),
            displayName = Str(node, "displayName"),
            difficulty = (AiDifficulty)Int(node, "difficulty"),
            slimeColor = Str(node, "slimeColor")
        };
    }

    // 방의 기본 정보만 (점수, 최종 판, 채팅은 따로 씀)
    public static Dictionary<string, object> FromRoom(RoomInfo room)
    {
        Dictionary<string, object> slots = new Dictionary<string, object>();
        for (int i = 0; i < room.slots.Length; i++)
        {
            slots[i.ToString()] = FromSlot(room.slots[i]);
        }

        return new Dictionary<string, object>
        {
            { "roomName", room.roomName ?? "" },
            { "hostUid", room.hostUid ?? "" },
            { "maxPlayers", room.maxPlayers },
            { "state", (int)room.state },
            { "seed", room.seed },
            { "targetSum", room.targetSum },
            { "startAt", room.startAt },
            { "playAt", room.playAt },
            { "slots", slots }
        };
    }

    public static RoomInfo ToRoom(string roomId, object node)
    {
        RoomInfo room = new RoomInfo
        {
            roomId = roomId,
            roomName = Str(node, "roomName"),
            hostUid = Str(node, "hostUid"),
            maxPlayers = Int(node, "maxPlayers", GameRules.MaxPlayers),
            state = (RoomState)Int(node, "state"),
            seed = Int(node, "seed"),
            targetSum = Int(node, "targetSum"),
            startAt = Long(node, "startAt"),
            playAt = Long(node, "playAt"),
            slots = new SlotInfo[GameRules.MaxPlayers]
        };

        Dictionary<string, object> slots = Child(node, "slots");
        for (int i = 0; i < room.slots.Length; i++)
        {
            room.slots[i] = ToSlot(Get(slots, i.ToString()));
        }
        return room;
    }

    public static Dictionary<string, object> FromScore(ScoreEntry entry)
    {
        return new Dictionary<string, object>
        {
            { "slot", entry.slot },
            { "score", entry.score },
            { "lastScoreTime", entry.lastScoreTime },
            { "forfeited", entry.forfeited },
            { "forfeitTime", entry.forfeitTime }
        };
    }

    public static ScoreEntry ToScore(object node)
    {
        return new ScoreEntry
        {
            slot = Int(node, "slot"),
            score = Int(node, "score"),
            lastScoreTime = Float(node, "lastScoreTime"),
            forfeited = Bool(node, "forfeited"),
            forfeitTime = Float(node, "forfeitTime")
        };
    }

    // 점수 노드들을 자리 순서로
    public static List<ScoreEntry> ToScores(object scoresNode)
    {
        List<ScoreEntry> entries = new List<ScoreEntry>();
        if (scoresNode is Dictionary<string, object> dictionary)
        {
            foreach (object node in dictionary.Values)
            {
                entries.Add(ToScore(node));
            }
        }
        entries.Sort((a, b) => a.slot.CompareTo(b.slot));
        return entries;
    }

    // 보낼 채팅. 시각은 서버가 채움.
    public static Dictionary<string, object> FromChat(string uid, string displayName, string text, ChatChannel channel)
    {
        return new Dictionary<string, object>
        {
            { "uid", uid },
            { "displayName", displayName },
            { "text", text },
            { "channel", (int)channel },
            { "time", ServerTimestamp }
        };
    }

    public static ChatMessage ToChat(object node, out ChatChannel channel)
    {
        channel = (ChatChannel)Int(node, "channel");
        return new ChatMessage
        {
            uid = Str(node, "uid"),
            displayName = Str(node, "displayName"),
            text = Str(node, "text"),
            time = Long(node, "time")
        };
    }
}
