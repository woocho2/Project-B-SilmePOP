using UnityEngine;
using UnityEngine.UI;

// 대기실 패널. RobbyPanel에 붙임. 지금 방의 자리 카드를 그리고, 방장의 AI 추가·제거·난이도 변경과 게임 시작을 서버로 보냄.
// 방이 바뀌면 NetworkManager.RoomChanged로 새 정보가 와서 다시 그림.
// 시작하면 모두에게 MatchStarting이 오고 UIManager_Menu가 게임 씬으로 넘어감. 나가면 UIManager_Menu가 메뉴로 돌아감.
public class RobbyPanel : MonoBehaviour
{
    [Tooltip("자리 카드. 순서대로 PLAYER 1 ~ 4")]
    [SerializeField] private RoomSlotItem[] slotItems = new RoomSlotItem[0];
    [Tooltip("방장만 누를 수 있음. 준비 없이 바로 시작")]
    [SerializeField] private Button btn_startGame;
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
            if (NetworkManager.Instance != null) NetworkManager.Instance.LeaveRoom();
        });
    }

    private void OnEnable()
    {
        if (NetworkManager.Instance == null) return;

        NetworkManager.Instance.RoomChanged += Refresh;
        Refresh(NetworkManager.Instance.CurrentRoom);
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null) NetworkManager.Instance.RoomChanged -= Refresh;
    }

    private void Refresh(RoomInfo room)
    {
        bool isHost = NetworkManager.Instance != null && NetworkManager.Instance.IsHost;

        for (int i = 0; i < slotItems.Length; i++)
        {
            if (slotItems[i] == null) continue;

            SlotInfo info = room != null && i < room.slots.Length ? room.slots[i] : null;
            bool isOpen = room != null && i < room.maxPlayers;
            slotItems[i].Show(i, info, isOpen, isHost);
        }

        if (btn_startGame != null) btn_startGame.interactable = isHost && room != null && room.state == RoomState.Waiting;
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
