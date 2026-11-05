using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 채팅창 하나 (로비는 Channel = Lobby, 대기실은 Room). Enter나 전송 버튼으로 보내고, 받은 메시지를 아래에 쌓음.
// 씬이 바뀌어 새로 생겨도 NetworkManager가 들고 있는 지난 대화부터 보여 줌.
// 메시지 줄 프리팹이 없으면 입력칸과 같은 글꼴로 줄을 만듦.
// 접는 채팅(로비): Lobby_ChatPreview가 Open으로 펼치고, 채팅창 바깥을 누르거나 ESC로 접힘(Closed).
public class ChatPanel : MonoBehaviour
{
    private const int MaxLines = 100;

    [SerializeField] private ChatChannel channel = ChatChannel.Room;
    [SerializeField] private ScrollRect scroll;
    [Tooltip("Scroll View의 Content. 메시지가 한 줄씩 여기 아래에 생김")]
    [SerializeField] private RectTransform content;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private Button btn_send;
    [Tooltip("선택 사항. 메시지 한 줄 프리팹. 비우면 입력칸 글꼴로 만듦")]
    [SerializeField] private TMP_Text messagePrefab;
    [SerializeField] private Color systemMessageColor = new Color(0.85f, 0.35f, 0.2f);

    private readonly Queue<GameObject> lines = new Queue<GameObject>();
    private string currentRoomId;

    // 채팅 입력칸에 글을 쓰는 중인가. 스킬 단축키(1 ~ 6)·ESC를 막는 데 씀 (기획 [4-1]).
    public static bool IsTyping => typingPanel != null && typingPanel.input != null && typingPanel.input.isFocused;
    private static ChatPanel typingPanel;

    // 접혔음. 이것을 듣는 쪽(미리보기)이 있으면 채팅창 바깥을 누르거나 ESC로 접힘
    public event Action Closed;

    public ChatChannel Channel => channel;

    private int openedFrame = -1;   // 펼친 그 클릭이 곧바로 "바깥 클릭"으로 잡혀 다시 접히지 않게

    private void Update()
    {
        if (input != null && input.isFocused) typingPanel = this;
        else if (typingPanel == this) typingPanel = null;

        if (Closed != null && (EscapePressed() || PressedOutside())) Close();
    }

    private static bool EscapePressed()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    // 마우스·터치를 채팅창 사각형 바깥에서 눌렀는가
    private bool PressedOutside()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame || Time.frameCount == openedFrame) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, pointer.position.ReadValue(), eventCamera);
    }

    // 펼치고 바로 칠 수 있게 입력칸에 커서를 둠
    public void Open()
    {
        openedFrame = Time.frameCount;
        PanelTransition.Show(gameObject);
        ScrollToBottom();
        if (input != null) input.ActivateInputField();
    }

    public void Close()
    {
        if (!PanelTransition.IsShown(gameObject)) return;   // 이미 접혔거나 접히는 중
        PanelTransition.Hide(gameObject);
        Closed?.Invoke();
    }

    // 한 줄 글자: 시스템 메시지는 그대로, 아니면 "닉네임: 내용"
    public static string Format(ChatMessage message)
    {
        return message.IsSystem ? message.text : $"{TextLines.Nickname(message.displayName)}: {message.text}";
    }

    private void Awake()
    {
        FindMissingParts();
        TextLines.PrepareLayout(content);

        if (input != null)
        {
            input.lineType = TMP_InputField.LineType.SingleLine;   // Enter = 보내기
            input.characterLimit = GameRules.ChatMaxLength;
            input.onSubmit.AddListener(_ => Send());
        }
        if (btn_send != null) btn_send.onClick.AddListener(Send);

        NetworkManager network = NetworkManager.Instance;
        if (network == null) return;

        currentRoomId = network.CurrentRoom != null ? network.CurrentRoom.roomId : null;
        foreach (ChatMessage message in network.ChatHistory(channel))
        {
            OnChatReceived(channel, message);
        }
        network.ChatReceived += OnChatReceived;
        network.RoomChanged += OnRoomChanged;
    }

    private void OnDestroy()
    {
        if (typingPanel == this) typingPanel = null;
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.ChatReceived -= OnChatReceived;
        NetworkManager.Instance.RoomChanged -= OnRoomChanged;
    }

    private void Send()
    {
        if (input == null || NetworkManager.Instance == null) return;

        // 빈 글이거나 1초 안에 또 보내면 false (그때는 입력한 글을 남겨 둠)
        if (NetworkManager.Instance.SendChat(channel, input.text))
        {
            SoundManager.Play(SoundEffect.Click);
            input.text = string.Empty;
        }
        input.ActivateInputField();   // 보낸 뒤에도 바로 이어서 칠 수 있게
    }

    private void OnChatReceived(ChatChannel received, ChatMessage message)
    {
        if (received != channel) return;

        AddLine(Format(message), message.IsSystem);
    }

    // 방 채팅은 다른 방에 들어가거나 방에서 나오면 이전 방의 대화를 지움 (로비 채팅은 그대로)
    private void OnRoomChanged(RoomInfo room)
    {
        if (channel == ChatChannel.Lobby) return;

        string roomId = room != null ? room.roomId : null;
        if (roomId == currentRoomId) return;

        currentRoomId = roomId;
        Clear();
    }

    private void AddLine(string text, bool isSystem)
    {
        if (content == null) return;

        TMP_Text line = TextLines.Create(content, messagePrefab, input != null ? input.textComponent : null, text);
        if (isSystem) line.color = systemMessageColor;

        lines.Enqueue(line.gameObject);
        while (lines.Count > MaxLines) Destroy(lines.Dequeue());

        ScrollToBottom();
    }

    // 레이아웃이 줄 높이를 계산한 뒤 맨 아래로 내림 (접혀 있는 동안 온 메시지는 펼칠 때 내림)
    private void ScrollToBottom()
    {
        if (!isActiveAndEnabled) return;
        Canvas.ForceUpdateCanvases();
        if (scroll != null) scroll.verticalNormalizedPosition = 0f;
    }

    private void Clear()
    {
        while (lines.Count > 0)
        {
            GameObject line = lines.Dequeue();
            if (line != null) Destroy(line);
        }
    }

    // 비운 칸은 자식에서 찾음: 스크롤 뷰, 그 Content, 입력칸, 전송 버튼 (채팅창 안에 각각 하나씩 있다고 봄)
    private void FindMissingParts()
    {
        if (scroll == null) scroll = GetComponentInChildren<ScrollRect>(true);
        if (content == null && scroll != null) content = scroll.content;
        if (input == null) input = GetComponentInChildren<TMP_InputField>(true);
        if (btn_send == null) btn_send = GetComponentInChildren<Button>(true);

        if (content == null || input == null)
        {
            Debug.LogWarning($"ChatPanel: {name} 아래에서 스크롤 뷰나 입력칸을 찾지 못했습니다.", this);
        }
    }
}
