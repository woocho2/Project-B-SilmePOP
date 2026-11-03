using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 채팅창 하나 (대기실 등에서 재사용). Enter나 전송 버튼으로 보내고, 받은 메시지를 아래에 쌓음.
// 메시지 줄 프리팹이 없으면 입력칸과 같은 글꼴로 줄을 만듦. Content에 줄을 쌓는 레이아웃이 없으면 붙임.
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

    private void Update()
    {
        if (input != null && input.isFocused) typingPanel = this;
        else if (typingPanel == this) typingPanel = null;
    }

    private void Awake()
    {
        FindMissingParts();
        PrepareContentLayout();

        if (input != null)
        {
            input.lineType = TMP_InputField.LineType.SingleLine;   // Enter = 보내기
            input.characterLimit = GameRules.ChatMaxLength;
            input.onSubmit.AddListener(_ => Send());
        }
        if (btn_send != null) btn_send.onClick.AddListener(Send);

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.ChatReceived += OnChatReceived;
            NetworkManager.Instance.RoomChanged += OnRoomChanged;
            OnRoomChanged(NetworkManager.Instance.CurrentRoom);
        }
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

        string text = message.IsSystem ? message.text : $"{Nickname(message.displayName)}: {message.text}";
        AddLine(text, message.IsSystem);
    }

    // 다른 방에 들어가거나 방에서 나오면 이전 방의 대화를 지움
    private void OnRoomChanged(RoomInfo room)
    {
        string roomId = room != null ? room.roomId : null;
        if (roomId == currentRoomId) return;

        currentRoomId = roomId;
        Clear();
    }

    private void AddLine(string text, bool isSystem)
    {
        if (content == null) return;

        TMP_Text line = CreateLine();
        line.text = text;
        if (isSystem) line.color = systemMessageColor;

        lines.Enqueue(line.gameObject);
        while (lines.Count > MaxLines) Destroy(lines.Dequeue());

        // 레이아웃이 줄 높이를 계산한 뒤 맨 아래로 내림
        Canvas.ForceUpdateCanvases();
        if (scroll != null) scroll.verticalNormalizedPosition = 0f;
    }

    private TMP_Text CreateLine()
    {
        if (messagePrefab != null) return Instantiate(messagePrefab, content);

        GameObject go = new GameObject("Message", typeof(RectTransform));
        go.transform.SetParent(content, false);
        TextMeshProUGUI line = go.AddComponent<TextMeshProUGUI>();
        TMP_Text style = input != null ? input.textComponent : null;
        if (style != null)
        {
            line.font = style.font;
            line.fontSize = style.fontSize;
            line.color = style.color;
        }
        line.textWrappingMode = TextWrappingModes.Normal;
        line.raycastTarget = false;
        return line;
    }

    private void Clear()
    {
        while (lines.Count > 0)
        {
            GameObject line = lines.Dequeue();
            if (line != null) Destroy(line);
        }
    }

    // 비운 칸은 자식에서 찾음: 스크롤 뷰, 그 Content, 입력칸, 버튼 (채팅창 안에 각각 하나씩 있다고 봄)
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

    // Content가 줄 높이만큼 늘어나며 위에서부터 쌓이게 함
    private void PrepareContentLayout()
    {
        if (content == null) return;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 2f;
        }
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // "닉네임#0012" → "닉네임"
    private static string Nickname(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return "";
        int tag = displayName.LastIndexOf('#');
        return tag > 0 ? displayName.Substring(0, tag) : displayName;
    }
}
