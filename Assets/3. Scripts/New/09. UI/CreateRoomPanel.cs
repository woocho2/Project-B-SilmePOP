using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 방 만들기 패널: 제목, 인원 토글, 생성하기, 닫기. CreatePanel에 붙임.
// 열릴 때마다 제목을 비우고 토글을 모두 끔. 제목이 있고 인원을 골라야 생성하기를 누를 수 있음.
public class CreateRoomPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField input_roomName;
    [Tooltip("인원수 토글 Player1 ~ 4. 토글(또는 자식)의 글자에 적힌 숫자가 방 최대 인원이 됨")]
    [SerializeField] private Toggle[] toggles_maxPlayers = new Toggle[0];
    [SerializeField] private Button btn_createRoomConfirm;
    [Tooltip("방을 만들지 않고 메뉴로 돌아감")]
    [SerializeField] private Button btn_createRoomClose;

    private void Awake()
    {
        UIButtons.Bind(btn_createRoomConfirm, SubmitCreateRoom);
        UIButtons.Bind(btn_createRoomClose, () => UIManager_Menu.Instance.ShowMainMenu());
        if (input_roomName != null) input_roomName.onValueChanged.AddListener(_ => UpdateCreateButton());

        foreach (Toggle toggle in toggles_maxPlayers)
        {
            if (toggle == null) continue;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(isOn => OnMaxPlayersToggled(toggle, isOn));
        }
    }

    private void OnEnable()
    {
        if (input_roomName != null) input_roomName.SetTextWithoutNotify(string.Empty);
        foreach (Toggle toggle in toggles_maxPlayers)
        {
            if (toggle != null) toggle.SetIsOnWithoutNotify(false);
        }
        UpdateCreateButton();
    }

    // 하나를 켜면 나머지는 끔
    private void OnMaxPlayersToggled(Toggle changed, bool isOn)
    {
        if (isOn)
        {
            foreach (Toggle toggle in toggles_maxPlayers)
            {
                if (toggle != null && toggle != changed) toggle.SetIsOnWithoutNotify(false);
            }
        }
        UpdateCreateButton();
    }

    private void UpdateCreateButton()
    {
        UIButtons.SetInteractable(btn_createRoomConfirm, !string.IsNullOrWhiteSpace(RoomNameInput()) && SelectedMaxPlayers() > 0);
    }

    private string RoomNameInput()
    {
        return input_roomName != null ? input_roomName.text : string.Empty;
    }

    // 켜진 토글의 글자에서 숫자만 읽음 ("4", "4명" 모두 4). 켜진 게 없거나 못 읽으면 0.
    private int SelectedMaxPlayers()
    {
        foreach (Toggle toggle in toggles_maxPlayers)
        {
            if (toggle == null || !toggle.isOn) continue;

            TMP_Text label = toggle.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return 0;

            int number = 0;
            foreach (char c in label.text)
            {
                if (char.IsDigit(c)) number = number * 10 + (c - '0');
            }
            return number;
        }
        return 0;
    }

    private void SubmitCreateRoom()
    {
        NetworkManager network = NetworkManager.SignedIn(this);
        if (network == null) return;

        string roomName = RoomNameInput();
        int maxPlayers = SelectedMaxPlayers();
        if (string.IsNullOrWhiteSpace(roomName) || maxPlayers <= 0) return;

        UIButtons.SetInteractable(btn_createRoomConfirm, false);
        network.CreateRoom(roomName, maxPlayers, room =>
        {
            if (this == null) return;   // 응답이 오기 전에 씬이 바뀜
            UpdateCreateButton();
            if (room != null) UIManager_Menu.Instance.ShowLobby();   // 실패 사유는 NetworkManager.ErrorOccurred로 옴
        });
    }
}
