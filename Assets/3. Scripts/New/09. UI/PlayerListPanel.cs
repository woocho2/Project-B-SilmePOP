using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 로비의 접속 플레이어 목록 팝업. 공책 그림(UI_Paper_Torn_Notebook)의 파란 줄 위에 한 줄씩 "닉네임 #0001"을 적음.
// 맨 위 칸(첫 줄 위)에는 "접속자 N명". 누가 접속하거나 나가면 다시 그림 (15초 넘게 소식이 없는 사람은 빠짐).
// 줄 위치는 그림에서 잰 비율이라 공책(page) 크기를 바꿔도 줄에 맞음.
// 공책 바깥을 누르거나 ESC를 누르면 닫힘. 여는 것은 UIManager_Lobby의 Btn_PlayerList.
public class PlayerListPanel : MonoBehaviour
{
    // 공책 그림(1086 × 1448)에서 잰 파란 줄의 높이(px, 위에서부터) 24개
    private const float ImageWidth = 1086f;
    private const float ImageHeight = 1448f;
    private static readonly float[] LineY =
    {
        158f, 215.5f, 270f, 323.5f, 376.5f, 431.5f, 487f, 536.5f, 588.5f, 643.5f, 695f, 748.5f,
        799.5f, 853f, 906f, 958f, 1011f, 1064.5f, 1116f, 1168.5f, 1221.5f, 1274.5f, 1323.5f, 1374f
    };
    private const float HeaderTopY = 95f;              // 맨 위 칸(인원 수)의 윗선
    private const float TextLeftX = 100f;              // 스프링 고리 오른쪽, 줄이 시작하는 곳보다 조금 안
    private const float TextRightX = 1040f;

    [Tooltip("공책 그림 Image (PlayerList_BG). 비우면 자식에서 찾음")]
    [SerializeField] private RectTransform page;
    [Tooltip("줄 글자의 본보기 TMP (글꼴·크기·색). 실행하면 숨기고 줄마다 복사함. 비우면 공책 안의 첫 TMP")]
    [SerializeField] private TMP_Text rowStyle;
    [SerializeField] private Color myColor = new Color(0.2f, 0.45f, 0.9f);

    private readonly List<TMP_Text> rows = new List<TMP_Text>();
    private TMP_Text header;
    private int openedFrame = -1;

    private void Awake()
    {
        if (page == null)
        {
            foreach (UnityEngine.UI.Image image in GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.gameObject != gameObject) { page = image.rectTransform; break; }
            }
        }
        if (page == null)
        {
            Debug.LogWarning("PlayerListPanel: 공책 그림(page)을 찾지 못했습니다.", this);
            return;
        }
        if (rowStyle == null) rowStyle = page.GetComponentInChildren<TMP_Text>(true);
        if (rowStyle == null) rowStyle = CreateDefaultStyle();

        rowStyle.gameObject.SetActive(false);
        header = CreateRow(HeaderTopY, LineY[0]);
        for (int i = 0; i + 1 < LineY.Length; i++) rows.Add(CreateRow(LineY[i], LineY[i + 1]));   // 24줄 → 23칸
    }

    private void OnEnable()
    {
        openedFrame = Time.frameCount;
        if (NetworkManager.Instance == null) return;

        NetworkManager.Instance.LobbyUsersChanged += Refresh;
        Refresh(NetworkManager.Instance.LobbyUsers);
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null) NetworkManager.Instance.LobbyUsersChanged -= Refresh;
    }

    private void Update()
    {
        if (!PanelTransition.IsShown(gameObject)) return;
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (escape || PressedOutsidePage()) PanelTransition.Hide(gameObject);
    }

    // 본보기 글자가 없으면 화면의 다른 글자(한글이 나오는 글꼴)를 빌려 기본 모양을 만듦
    private TMP_Text CreateDefaultStyle()
    {
        GameObject go = new GameObject("RowStyle", typeof(RectTransform));
        go.transform.SetParent(page, false);
        TextMeshProUGUI style = go.AddComponent<TextMeshProUGUI>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            foreach (TMP_Text other in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (other != style && other.font != null) { style.font = other.font; break; }
            }
        }
        style.fontSize = 28f;
        style.color = new Color(0.2f, 0.2f, 0.2f);
        return style;
    }

    // 두 줄 사이 칸 하나에 글자를 둠. 위치를 그림 비율(앵커)로 잡아서 공책 크기가 바뀌어도 칸에 맞음.
    private TMP_Text CreateRow(float topY, float bottomY)
    {
        TMP_Text row = Instantiate(rowStyle, page);
        row.gameObject.SetActive(true);
        row.name = "Row";
        row.text = string.Empty;
        row.raycastTarget = false;
        row.textWrappingMode = TextWrappingModes.NoWrap;
        row.overflowMode = TextOverflowModes.Ellipsis;
        row.alignment = TextAlignmentOptions.BottomLeft;   // 줄 바로 위에 앉게
        row.margin = new Vector4(0f, 0f, 0f, 2f);

        // 칸이 작아지면 글자도 작아지게 (본보기 크기가 최대)
        row.enableAutoSizing = true;
        row.fontSizeMax = rowStyle.fontSize;
        row.fontSizeMin = Mathf.Min(10f, rowStyle.fontSize);

        RectTransform rect = row.rectTransform;
        rect.anchorMin = new Vector2(TextLeftX / ImageWidth, 1f - bottomY / ImageHeight);
        rect.anchorMax = new Vector2(TextRightX / ImageWidth, 1f - topY / ImageHeight);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return row;
    }

    private void Refresh(IReadOnlyList<UserInfo> users)
    {
        if (header == null) return;

        header.text = $"접속자 {users.Count}명";
        string myUid = NetworkManager.Instance.CurrentUser != null ? NetworkManager.Instance.CurrentUser.uid : null;

        // 칸보다 많으면 마지막 칸은 "외 N명"
        bool overflow = users.Count > rows.Count;
        int shown = overflow ? rows.Count - 1 : users.Count;
        for (int i = 0; i < rows.Count; i++)
        {
            TMP_Text row = rows[i];
            row.color = rowStyle.color;
            if (i < shown)
            {
                UserInfo user = users[i];
                bool isMe = user.uid == myUid;
                row.text = $"{user.nickname} #{UserInfo.FormatGuestNumber(user.guestNumber)}" + (isMe ? " (나)" : "");
                if (isMe) row.color = myColor;
            }
            else
            {
                row.text = overflow && i == rows.Count - 1 ? $"외 {users.Count - shown}명" : string.Empty;
            }
        }
    }

    // 공책 바깥(어두운 배경)을 눌렀는가. 연 그 클릭은 무시함
    private bool PressedOutsidePage()
    {
        Pointer pointer = Pointer.current;
        if (page == null || pointer == null || !pointer.press.wasPressedThisFrame || Time.frameCount == openedFrame) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return !RectTransformUtility.RectangleContainsScreenPoint(page, pointer.position.ReadValue(), eventCamera);
    }
}
