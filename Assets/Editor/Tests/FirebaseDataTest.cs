using System.Collections.Generic;
using NUnit.Framework;

public class FirebaseDataTest
{
    [Test]
    public void Json_RoundTripsNestedValues()
    {
        string json = "{\"a\":[1,2.5,\"x\\\"y\\n\"],\"b\":{\"c\":true,\"d\":null},\"e\":-3,\"f\":\"한글\\u0041\"}";

        object parsed = Json.Parse(json);
        Dictionary<string, object> root = (Dictionary<string, object>)parsed;
        List<object> a = (List<object>)root["a"];

        Assert.AreEqual(1L, a[0]);
        Assert.AreEqual(2.5, a[1]);
        Assert.AreEqual("x\"y\n", a[2]);
        Assert.AreEqual(true, ((Dictionary<string, object>)root["b"])["c"]);
        Assert.IsNull(((Dictionary<string, object>)root["b"])["d"]);
        Assert.AreEqual(-3L, root["e"]);
        Assert.AreEqual("한글A", root["f"]);
        Assert.AreEqual(Json.Serialize(parsed), Json.Serialize(Json.Parse(Json.Serialize(parsed))));
    }

    [Test]
    public void Json_KeepsLargeTimestampsExact()
    {
        Dictionary<string, object> root = (Dictionary<string, object>)Json.Parse("{\"t\":1793952000123}");

        Assert.AreEqual(1793952000123L, root["t"]);
    }

    [Test]
    public void ApplyPut_AndPatch_FollowStreamEvents()
    {
        object root = FirebaseData.ApplyPut(null, "/", Json.Parse("{\"r1\":{\"roomName\":\"방\",\"slots\":[{\"kind\":1},{\"kind\":0}]}}"));

        root = FirebaseData.ApplyPut(root, "/r1/slots/1", Json.Parse("{\"kind\":2}"));
        root = FirebaseData.ApplyPatch(root, "/r1", Json.Parse("{\"state\":1,\"roomName\":\"새 방\"}"));
        root = FirebaseData.ApplyPut(root, "/r2", Json.Parse("{\"roomName\":\"둘째\"}"));
        root = FirebaseData.ApplyPut(root, "/r2", null);

        Dictionary<string, object> r1 = FirebaseData.Child(root, "r1");
        Assert.AreEqual(2, FirebaseData.Int(FirebaseData.Child(FirebaseData.Child(r1, "slots"), "1"), "kind"));
        Assert.AreEqual("새 방", FirebaseData.Str(r1, "roomName"));
        Assert.AreEqual(1, FirebaseData.Int(r1, "state"));
        Assert.IsNull(FirebaseData.Get(root, "r2"));
    }

    [Test]
    public void Room_RoundTripsThroughNode()
    {
        RoomInfo room = new RoomInfo
        {
            roomName = "방",
            hostUid = "host",
            maxPlayers = 3,
            state = RoomState.Playing,
            seed = 42,
            targetSum = 13,
            startAt = 1793952000123L,
            slots = new SlotInfo[GameRules.MaxPlayers]
        };
        for (int i = 0; i < room.slots.Length; i++) room.slots[i] = new SlotInfo { kind = SlotKind.Empty };
        room.slots[0] = new SlotInfo { kind = SlotKind.Human, uid = "host", displayName = "슬라임#0001", slimeColor = "#B3270B" };
        room.slots[2] = new SlotInfo { kind = SlotKind.Ai, displayName = "AI 3", difficulty = AiDifficulty.Hell };

        object node = FirebaseData.Normalize(Json.Parse(Json.Serialize(FirebaseData.FromRoom(room))));
        RoomInfo back = FirebaseData.ToRoom("r1", node);

        Assert.AreEqual("r1", back.roomId);
        Assert.AreEqual(3, back.maxPlayers);
        Assert.AreEqual(RoomState.Playing, back.state);
        Assert.AreEqual(1793952000123L, back.startAt);
        Assert.AreEqual("#B3270B", back.slots[0].slimeColor);
        Assert.AreEqual(AiDifficulty.Hell, back.slots[2].difficulty);
        Assert.AreEqual(SlotKind.Empty, back.slots[3].kind);
    }

    [Test]
    public void Scores_ComeBackInSlotOrder()
    {
        Dictionary<string, object> scores = new Dictionary<string, object>
        {
            { FirebaseData.ScoreKey(2), FirebaseData.FromScore(new ScoreEntry { slot = 2, score = 7 }) },
            { FirebaseData.ScoreKey(0), FirebaseData.FromScore(new ScoreEntry { slot = 0, score = 5, lastScoreTime = 1.5f }) }
        };

        List<ScoreEntry> entries = FirebaseData.ToScores(FirebaseData.Normalize(Json.Parse(Json.Serialize(scores))));

        Assert.AreEqual(0, entries[0].slot);
        Assert.AreEqual(1.5f, entries[0].lastScoreTime);
        Assert.AreEqual(7, entries[1].score);
    }
}
