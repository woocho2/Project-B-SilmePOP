using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 로비의 접힌 채팅: 평소에는 가장 최근 메시지 한 줄만 보여 주고, 누르거나 Enter를 치면 채팅창(ChatPanel)을 펼침.
// 채팅창이 닫히면(채팅창 바깥 클릭·ESC) 다시 한 줄로 돌아옴. 옵션·접속자 목록이 떠 있는 동안에는 숨음(SetBlocked). 접혀 있는 동안 온 메시지 수를 배지로 보여 줌.
// 채팅창의 자식이 아니라 옆(형제)에 두어야 함 (채팅창을 꺼도 이것은 켜져 있어야 하므로).
public class Lobby_ChatPreview : MonoBehaviour
{
    [Tooltip("펼칠 채팅창")]
    [SerializeField] private ChatPanel chatPanel;
    [Tooltip("한 줄 미리보기 전체를 덮는 버튼. 누르면 펼침")]
    [SerializeField] private Button btn_open;
    [Tooltip("가장 최근 메시지 한 줄")]
    [SerializeField] private TMP_Text txt_lastMessage;
    [SerializeField] private string emptyMessage = "눌러서 채팅하기 (Enter)";

    [Header("안 읽은 메시지 (선택 사항)")]
    [Tooltip("안 읽은 메시지가 있을 때만 켜지는 배지")]
    [SerializeField] private GameObject obj_unreadBadge;
    [SerializeField] private TMP_Text txt_unreadCount;

    private int unreadCount;
    private bool blocked;   // 옵션·접속자 목록 같은 창이 떠 있어서 숨김

    private void Awake()
    {
        UIButtons.Bind(btn_open, Open);
        if (txt_lastMessage != null)
        {
            txt_lastMessage.textWrappingMode = TextWrappingModes.NoWrap;   // 한 줄만, 넘치면 …
            txt_lastMessage.overflowMode = TextOverflowModes.Ellipsis;
        }

        NetworkManager network = NetworkManager.Instance;
        ChatMessage last = null;
        if (network != null && chatPanel != null)
        {
            var history = network.ChatHistory(chatPanel.Channel);
            if (history.Count > 0) last = history[history.Count - 1];
            network.ChatReceived += OnChatReceived;
        }
        ShowLastMessage(last);
        SetUnread(0);

        if (chatPanel != null) chatPanel.Closed += OnChatClosed;
    }

    private void Start()
    {
        if (chatPanel == null)
        {
            Debug.LogError("Lobby_ChatPreview: chatPanel을 연결해 주세요.", this);
            return;
        }
        PanelTransition.SetImmediate(chatPanel.gameObject, false);   // 처음에는 접힌 상태
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null) NetworkManager.Instance.ChatReceived -= OnChatReceived;
        if (chatPanel != null) chatPanel.Closed -= OnChatClosed;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (!keyboard.enterKey.wasPressedThisFrame && !keyboard.numpadEnterKey.wasPressedThisFrame) return;
        if (IsTypingSomewhere()) return;   // 방 제목 입력칸 등에서 친 Enter는 그 칸의 것

        Open();
    }

    private void Open()
    {
        if (chatPanel == null) return;
        SoundManager.Play(SoundEffect.Click);
        SetUnread(0);
        PanelTransition.SetImmediate(gameObject, false);   // 펼쳐지는 채팅창이 그 자리를 덮으므로 바로 숨김
        chatPanel.Open();
    }

    private void OnChatClosed()
    {
        if (!blocked) PanelTransition.Show(gameObject);
    }

    // 다른 창(옵션, 접속자 목록)이 떠 있는 동안 숨김. 펼친 채팅창도 접음. UIManager_Lobby가 부름.
    public void SetBlocked(bool isBlocked)
    {
        if (blocked == isBlocked) return;
        blocked = isBlocked;

        if (blocked)
        {
            if (chatPanel != null) chatPanel.Close();
            PanelTransition.Hide(gameObject);
        }
        else
        {
            PanelTransition.Show(gameObject);
        }
    }

    private void OnChatReceived(ChatChannel channel, ChatMessage message)
    {
        if (chatPanel == null || channel != chatPanel.Channel) return;

        ShowLastMessage(message);
        if (!chatPanel.gameObject.activeInHierarchy) SetUnread(unreadCount + 1);
    }

    private void ShowLastMessage(ChatMessage message)
    {
        if (txt_lastMessage != null) txt_lastMessage.text = message != null ? ChatPanel.Format(message) : emptyMessage;
    }

    private void SetUnread(int count)
    {
        unreadCount = count;
        if (obj_unreadBadge != null) obj_unreadBadge.SetActive(count > 0);
        if (txt_unreadCount != null) txt_unreadCount.text = count > 99 ? "99+" : count.ToString();
    }

    private static bool IsTypingSomewhere()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        TMP_InputField field = selected != null ? selected.GetComponent<TMP_InputField>() : null;
        return field != null && field.isFocused;
    }
}
