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
    public string slimeColor;       // 그 자리 슬라임 색 "#RRGGBB" (사람은 자기가 고른 색)

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
    public int maxPlayers;          // 열려 있는 자리 수 (1 ~ 4). slots는 항상 4칸이고, 이 수 이상의 자리는 닫힘
    public RoomState state;
    public int seed;
    public int targetSum;
    public long startAt;            // 대기실 카운트다운(5초)이 끝나고 게임 씬으로 넘어가는 서버 시각
    public long playAt;             // 모두 게임 씬에 모인 뒤 3, 2, 1을 시작하는 서버 시각. 0이면 아직 기다리는 중
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

    // 열린 자리(maxPlayers 안) 중 첫 빈자리. 없으면 -1.
    public int FindEmptySlot()
    {
        for (int i = 0; i < maxPlayers && i < slots.Length; i++)
        {
            if (slots[i].kind == SlotKind.Empty) return i;
        }
        return -1;
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

    // 사람이 아닌 게임이 띄운 안내 (예: "5초뒤 게임이 시작합니다")
    public bool IsSystem => string.IsNullOrEmpty(uid);
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
