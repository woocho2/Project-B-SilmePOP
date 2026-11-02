using System;

// 네트워크로 주고받는 자료형. JSON으로 바꿀 수 있게 값은 public 필드로 둠.

[Serializable]
public sealed class UserInfo
{
    public string uid;
    public string nickname;
    public int guestNumber;   // 게스트가 생성된 순서. 화면의 UID이자 # 뒤의 번호

    public UserInfo() { }

    public UserInfo(string uid, string nickname, int guestNumber)
    {
        this.uid = uid;
        this.nickname = nickname;
        this.guestNumber = guestNumber;
    }

    public static string FormatGuestNumber(int number) => number.ToString("D4");

    public string DisplayName => $"{nickname}#{FormatGuestNumber(guestNumber)}";
}

[Serializable]
public sealed class SlotInfo
{
    public SlotKind kind;
    public string uid;              // 사람만
    public string displayName;      // "닉네임#1234" 또는 "AI 2"
    public AiDifficulty difficulty; // AI만
    public bool ready;              // AI는 항상 true

    public SlotInfo Clone()
    {
        return (SlotInfo)MemberwiseClone();
    }
}

[Serializable]
public sealed class RoomInfo
{
    public string roomId;
    public string roomName;
    public string hostUid;
    public RoomState state;
    public int seed;
    public int targetSum;
    public long startAt;
    public SlotInfo[] slots;

    public int PlayerCount
    {
        get
        {
            int count = 0;
            foreach (SlotInfo slot in slots)
            {
                if (slot.kind != SlotKind.Empty) count++;
            }
            return count;
        }
    }

    // 방장을 뺀 사람이 모두 준비했는가. 방장은 시작 버튼을 누르는 쪽이라 준비가 없음.
    public bool IsEveryoneReady
    {
        get
        {
            foreach (SlotInfo slot in slots)
            {
                if (slot.kind == SlotKind.Human && slot.uid != hostUid && !slot.ready) return false;
            }
            return true;
        }
    }

    public int FindSlot(string uid)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].kind == SlotKind.Human && slots[i].uid == uid) return i;
        }
        return -1;
    }

    public RoomInfo Clone()
    {
        RoomInfo copy = (RoomInfo)MemberwiseClone();
        copy.slots = new SlotInfo[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            copy.slots[i] = slots[i].Clone();
        }
        return copy;
    }
}

[Serializable]
public sealed class ChatMessage
{
    public string uid;
    public string displayName;
    public string text;
    public long time;   // 서버 시각 (밀리초)
}

[Serializable]
public sealed class ScoreEntry
{
    public int slot;
    public int score;
    public float lastScoreTime;   // 경기 시작 기준 경과 시간. 동점이면 먼저 도달한 쪽이 위
    public bool forfeited;
    public float forfeitTime;     // 먼저 나간 사람이 더 아래

    public ScoreEntry Clone()
    {
        return (ScoreEntry)MemberwiseClone();
    }
}
