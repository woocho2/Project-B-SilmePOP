using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 게임룸 씬 (과제의 Room Scene)의 방 패널. RoomPanel에 붙임.
// 방 정보(제목, 인원)와 참가자 카드를 그리고, 방장의 AI 추가·제거·난이도 변경과 게임 시작, 나가기를 서버로 보냄.
// 방이 바뀌면 NetworkManager.RoomChanged로 새 정보가 와서 다시 그림. 입장·퇴장 안내는 NetworkManager가 방 채팅에 띄움.
// 씬 이동(나가면 로비, 시작하면 게임)은 UIManager_Room이 함.
public class RoomPanel : MonoBehaviour
{
    [Header("방 정보")]
    [SerializeField] private TMP_Text txt_roomName;
    [Tooltip("'2/4명 · 대기 중'처럼 인원과 상태")]
    [SerializeField] private TMP_Text txt_roomInfo;

    [Header("참가자")]
    [Tooltip("자리 카드. 순서대로 PLAYER 1 ~ 4")]
    [SerializeField] private RoomSlotItem[] slotItems = new RoomSlotItem[0];

    [Header("버튼")]
    [Tooltip("방장만 누를 수 있음. 준비 없이 바로 시작")]
    [SerializeField] private Button btn_startGame;
    [Tooltip("방에서 나가기. 나오면 UIManager_Room이 로비로 보냄")]
    [SerializeField] private Button btn_leaveRoom;

    private void Awake()
    {
        foreach (RoomSlotItem item in slotItems)
        {
            if (item == null) continue;
            item.AiToggled += OnAiToggled;
            item.DifficultyChanged += OnDifficultyChanged;
        }

        UIButtons.Bind(btn_startGame, () =>
        {
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost) return;
            btn_startGame.interactable = false;   // 두 번 눌리지 않게. 시작이 되면 씬이 넘어감
            NetworkManager.Instance.StartMatch();
        });
        UIButtons.Bind(btn_leaveRoom, () =>
        {
            if (NetworkManager.Instance != null) NetworkManager.Instance.LeaveRoom();   // 나오면 RoomChanged(null) → 로비로
        });
    }

    private void OnEnable()
    {
        if (NetworkManager.Instance == null) return;

        NetworkManager.Instance.RoomChanged += Refresh;
        NetworkManager.Instance.ErrorOccurred += OnError;
        Refresh(NetworkManager.Instance.CurrentRoom);
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.RoomChanged -= Refresh;
        NetworkManager.Instance.ErrorOccurred -= OnError;
    }

    // AI 넣기·빼기 등이 실패하면 방이 그대로라 RoomChanged가 오지 않음.
    // 누르자마자 바뀐 토글·드롭다운을 실제 방 상태로 되돌리고, 이유를 방 채팅에 띄움.
    private void OnError(string message)
    {
        NetworkManager network = NetworkManager.Instance;
        if (network == null) return;
        Refresh(network.CurrentRoom);
        if (network.CurrentRoom != null) network.ShowSystemMessage(ChatChannel.Room, message);
    }

    private void Refresh(RoomInfo room)
    {
        bool isHost = NetworkManager.Instance != null && NetworkManager.Instance.IsHost;

        if (room != null)
        {
            if (txt_roomName != null) txt_roomName.text = room.roomName;
            if (txt_roomInfo != null) txt_roomInfo.text = $"{room.PlayerCount}/{room.maxPlayers}명 · {StateText(room.state)}";
        }

        for (int i = 0; i < slotItems.Length; i++)
        {
            if (slotItems[i] == null) continue;

            SlotInfo info = room != null && i < room.slots.Length ? room.slots[i] : null;
            bool isOpen = room != null && i < room.maxPlayers;
            slotItems[i].Show(i, info, isOpen, isHost);
        }

        if (btn_startGame != null) btn_startGame.interactable = isHost && room != null && room.state == RoomState.Waiting;
    }

    private static string StateText(RoomState state)
    {
        switch (state)
        {
            case RoomState.Playing: return "게임 중";
            case RoomState.Result: return "결과";
            default: return "대기 중";
        }
    }

    private void OnAiToggled(int slot, bool isOn)
    {
        NetworkManager network = NetworkManager.Instance;
        if (network == null || !network.IsHost) return;

        if (isOn) network.AddAi(slot, AiDifficulty.Easy);
        else network.RemoveAi(slot);
    }

    private void OnDifficultyChanged(int slot, AiDifficulty difficulty)
    {
        NetworkManager network = NetworkManager.Instance;
        if (network == null || !network.IsHost) return;

        network.AddAi(slot, difficulty);   // AI가 있는 자리에 다시 부르면 난이도만 바뀜
    }
}
