using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 메뉴 패널: 방 목록, 방 만들기, 빠른 참여. MenuPanel에 붙임.
// 방 목록은 패널이 보이는 동안만 받아서 그림 (다시 보이면 마지막 목록으로 다시 그림).
public class MainMenuPanel : MonoBehaviour
{
    [Tooltip("방 목록 Scroll View_Rooms의 Content. 방마다 RoomItem이 하나씩 생기고, 방이 없어지면 지워짐")]
    [SerializeField] private Transform roomListContent;
    [Tooltip("RoomItem 프리팹 (RoomListItem이 붙어 있어야 함)")]
    [SerializeField] private RoomListItem roomItemPrefab;
    [SerializeField] private Button btn_makeRoom;     // 방 만들기 패널로
    [SerializeField] private Button btn_quickEntry;   // 대기 중인 아무 방에 들어감

    private readonly Dictionary<string, RoomListItem> roomItems = new Dictionary<string, RoomListItem>();

    private void Awake()
    {
        UIButtons.Bind(btn_makeRoom, () => UIManager_Menu.Instance.ShowCreateRoom());
        UIButtons.Bind(btn_quickEntry, QuickEntry);
    }

    private void OnEnable()
    {
        if (NetworkManager.Instance == null) return;

        NetworkManager.Instance.RoomListChanged += RefreshRoomList;
        RefreshRoomList(NetworkManager.Instance.Rooms);
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null) NetworkManager.Instance.RoomListChanged -= RefreshRoomList;
    }

    // 받은 방 목록에 맞춰 RoomItem을 만들고, 고치고, 없어진 방의 것은 지움
    private void RefreshRoomList(IReadOnlyList<RoomInfo> rooms)
    {
        if (roomListContent == null || roomItemPrefab == null) return;

        HashSet<string> alive = new HashSet<string>();
        foreach (RoomInfo room in rooms)
        {
            alive.Add(room.roomId);
            if (!roomItems.TryGetValue(room.roomId, out RoomListItem item) || item == null)
            {
                item = Instantiate(roomItemPrefab, roomListContent);
                item.Clicked += JoinRoom;
                roomItems[room.roomId] = item;
            }
            item.Show(room);
        }

        List<string> gone = new List<string>();
        foreach (KeyValuePair<string, RoomListItem> pair in roomItems)
        {
            if (!alive.Contains(pair.Key)) gone.Add(pair.Key);
        }
        foreach (string roomId in gone)
        {
            if (roomItems[roomId] != null) Destroy(roomItems[roomId].gameObject);
            roomItems.Remove(roomId);
        }
    }

    // 대기 중이고 빈자리가 있는 방 중 아무 곳
    private void QuickEntry()
    {
        NetworkManager network = NetworkManager.SignedIn(this);
        if (network == null) return;

        List<RoomInfo> candidates = new List<RoomInfo>();
        foreach (RoomInfo room in network.Rooms)
        {
            if (room.state == RoomState.Waiting && room.FindEmptySlot() >= 0) candidates.Add(room);
        }
        if (candidates.Count == 0)
        {
            Debug.Log("MainMenuPanel: 들어갈 수 있는 방이 없습니다.", this);
            return;
        }

        JoinRoom(candidates[Random.Range(0, candidates.Count)].roomId);
    }

    private void JoinRoom(string roomId)
    {
        NetworkManager network = NetworkManager.SignedIn(this);
        if (network == null) return;

        UIButtons.SetInteractable(btn_quickEntry, false);
        network.JoinRoom(roomId, room =>
        {
            if (this == null) return;   // 응답이 오기 전에 씬이 바뀜
            UIButtons.SetInteractable(btn_quickEntry, true);
            if (room != null) UIManager_Menu.Instance.ShowLobby();   // 실패 사유는 NetworkManager.ErrorOccurred로 옴
        });
    }
}
