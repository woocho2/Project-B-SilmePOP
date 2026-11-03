using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 메뉴 씬. 화면 위 버튼(종료·도움말·옵션)과 패널 켜고 끄기만 맡음. 각 패널의 일은 패널에 붙은 스크립트가 함.
// 메뉴 → 방 만들기 → 대기실(Robby)로 앞으로 가고, 옵션·도움말은 연 곳으로 돌아가며, 종료 확인은 지금 화면 위에 겹쳐 띄움.
// 방 상태에 따른 화면 전환(방에서 나오면 메뉴로, 시작하면 5초 세고 게임 씬으로)도 여기서 함.
public class UIManager_Menu : Singleton<UIManager_Menu>
{
    [Header("화면 위 버튼")]
    [SerializeField] private Button btn_quit;     // QuitPanel 띄움
    [SerializeField] private Button btn_help;     // HelpPanel 띄움
    [SerializeField] private Button btn_option;   // OptionPanel 띄움

    [Header("패널")]
    [SerializeField] private GameObject Panel_MainMenu;
    [SerializeField] private GameObject Panel_Quit;
    [SerializeField] private GameObject Panel_Option;
    [SerializeField] private GameObject Panel_CreateRoom;
    [SerializeField] private GameObject Panel_Help;
    [SerializeField] private GameObject Panel_Lobby;

    [Header("Scene Settings")]
    [Tooltip("방장이 게임을 시작하면 모두 이 씬으로 넘어감")]
    [SerializeField] private string m_sceneName;

    private GameObject currentPanel;
    private readonly Stack<GameObject> returnPanels = new Stack<GameObject>();
    private Sequence startCountdown;

    private void Start()
    {
        if (Instance != this) return;

        // 게임을 마치고 돌아왔으면 아직 방 안이므로 대기실로
        bool inRoom = NetworkManager.Instance != null && NetworkManager.Instance.CurrentRoom != null;
        GoTo(inRoom && Panel_Lobby != null ? Panel_Lobby : Panel_MainMenu);
        if (Panel_Quit != null) Panel_Quit.SetActive(false);

        UIButtons.Bind(btn_quit, () =>
        {
            if (Panel_Quit != null) Panel_Quit.SetActive(true);
            else QuitPopup.QuitGame();
        });
        UIButtons.Bind(btn_help, () => OpenSubPanel(Panel_Help));
        UIButtons.Bind(btn_option, () => OpenSubPanel(Panel_Option));

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.RoomChanged += OnRoomChanged;
            NetworkManager.Instance.MatchStarting += OnMatchStarting;
        }
    }

    protected override void OnDestroy()
    {
        startCountdown?.Kill();
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.RoomChanged -= OnRoomChanged;
            NetworkManager.Instance.MatchStarting -= OnMatchStarting;
        }
        base.OnDestroy();
    }

    // ── 패널이 부르는 이동 ──────────────────────────────────────

    public void ShowMainMenu() => GoTo(Panel_MainMenu);
    public void ShowCreateRoom() => GoTo(Panel_CreateRoom);

    // 방을 만들거나 들어간 뒤
    public void ShowLobby()
    {
        if (Panel_Lobby == null)
        {
            Debug.LogWarning("UIManager_Menu: 방에 들어갔지만 Panel_Lobby가 연결되지 않았습니다.", this);
            return;
        }
        GoTo(Panel_Lobby);
    }

    // 옵션·도움말 닫기: 열기 전 화면으로
    public void CloseSubPanel()
    {
        ShowOnly(returnPanels.Count > 0 ? returnPanels.Pop() : Panel_MainMenu);
    }

    // 메뉴 → 방 만들기 → 대기실처럼 앞으로 가는 이동. 돌아갈 곳 기록을 지움.
    private void GoTo(GameObject panel)
    {
        returnPanels.Clear();
        ShowOnly(panel);
    }

    // 옵션·도움말: 지금 화면을 기억해 두고 엶
    private void OpenSubPanel(GameObject panel)
    {
        if (panel == null || panel == currentPanel) return;
        if (currentPanel != null) returnPanels.Push(currentPanel);
        ShowOnly(panel);
    }

    private void ShowOnly(GameObject panel)
    {
        currentPanel = panel;
        GameObject[] panels = { Panel_MainMenu, Panel_Option, Panel_Help, Panel_CreateRoom, Panel_Lobby };
        foreach (GameObject p in panels)
        {
            if (p != null) p.SetActive(p == panel);
        }
    }

    // ── 방 상태에 따른 이동 ─────────────────────────────────────

    // 방에서 나왔으면 (나가기, 방이 없어짐, 끊겨서 빠짐) 대기실 대신 메뉴로
    private void OnRoomChanged(RoomInfo room)
    {
        if (room != null) return;

        startCountdown?.Kill();   // 세는 중에 방에서 나오면 게임 씬으로 넘어가지 않음
        if (currentPanel == Panel_Lobby || returnPanels.Contains(Panel_Lobby)) GoTo(Panel_MainMenu);
    }

    // 방장이 시작함: 대기실 채팅에 5, 4, 3, 2, 1을 띄운 뒤 모든 참가자가 게임 씬으로.
    // 서버 시각(startAt) 기준으로 세므로 모든 화면에서 같은 순간에 바뀜.
    private void OnMatchStarting(MatchSettings settings)
    {
        if (string.IsNullOrWhiteSpace(m_sceneName))
        {
            Debug.LogError("UIManager_Menu: 게임 씬 이름(m_sceneName)을 넣어 주세요.", this);
            return;
        }

        NetworkManager network = NetworkManager.Instance;
        float remaining = Mathf.Max(0f, (settings.StartAt - network.ServerNow) / 1000f);
        int from = Mathf.Min(Mathf.RoundToInt(GameRules.StartDelay), Mathf.FloorToInt(remaining));

        startCountdown?.Kill();
        startCountdown = DOTween.Sequence().SetUpdate(true);
        for (int seconds = from; seconds >= 1; seconds--)
        {
            string message = $"{seconds}초뒤 게임이 시작합니다.....";
            startCountdown.InsertCallback(remaining - seconds, () => network.ShowSystemMessage(ChatChannel.Room, message));
        }
        startCountdown.InsertCallback(remaining, () =>
        {
            startCountdown = null;
            SceneLoader.StartLoad(m_sceneName);
        });
    }
}
