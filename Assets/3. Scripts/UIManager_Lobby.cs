using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 로비 씬 (과제의 Lobby Scene). 화면 위 버튼(종료·도움말·옵션)과 패널 켜고 끄기, 씬 이동만 맡음.
// 각 패널의 일(방 목록, 방 만들기, 접속자, 채팅 등)은 패널에 붙은 스크립트가 함.
// 방을 만들거나 들어가면 Room 씬으로, 로그아웃하거나 접속이 끊기면 로그인(Title) 씬으로 감.
public class UIManager_Lobby : Singleton<UIManager_Lobby>
{
    [Header("화면 위 버튼")]
    [SerializeField] private Button btn_quit;     // QuitPanel 띄움
    [SerializeField] private Button btn_help;     // HelpPanel 띄움
    [SerializeField] private Button btn_option;   // OptionPanel 띄움
    [SerializeField] private Button btn_playerList;   // 접속자 목록(PlayerListPanel)을 켜고 끔

    [Header("패널")]
    [SerializeField] private GameObject Panel_MainMenu;
    [SerializeField] private GameObject Panel_Quit;
    [SerializeField] private GameObject Panel_Option;
    [SerializeField] private GameObject Panel_CreateRoom;
    [SerializeField] private GameObject Panel_Help;
    [Tooltip("접속자 목록 팝업. 지금 화면 위에 겹쳐 뜸")]
    [SerializeField] private GameObject Panel_PlayerList;

    [Header("채팅")]
    [Tooltip("한 줄 채팅 미리보기. 옵션·접속자 목록이 떠 있는 동안 숨김. 비우면 씬에서 찾음")]
    [SerializeField] private Lobby_ChatPreview chatPreview;

    [Header("Scene Settings")]
    [Tooltip("방을 만들거나 들어가면 이 씬으로. 예: 1. Scenes/3. Room")]
    [SerializeField] private string m_roomSceneName;
    [Tooltip("로그아웃하면 이 씬으로. 예: 1. Scenes/1. Title")]
    [SerializeField] private string m_loginSceneName;

    private GameObject currentPanel;
    private readonly Stack<GameObject> returnPanels = new Stack<GameObject>();
    private bool isLeavingScene;

    private void Start()
    {
        if (Instance != this) return;
        if (chatPreview == null) chatPreview = FindAnyObjectByType<Lobby_ChatPreview>(FindObjectsInactive.Include);

        // 처음에는 메인 메뉴만: 다른 패널은 바로 끄고 메뉴는 부드럽게 나타남
        foreach (GameObject panel in Panels)
        {
            if (panel != Panel_MainMenu) PanelTransition.SetImmediate(panel, false);
        }
        PanelTransition.SetImmediate(Panel_Quit, false);
        PanelTransition.SetImmediate(Panel_PlayerList, false);
        GoTo(Panel_MainMenu);

        UIButtons.Bind(btn_playerList, () =>
        {
            if (Panel_PlayerList == null) return;
            if (PanelTransition.IsShown(Panel_PlayerList)) PanelTransition.Hide(Panel_PlayerList);
            else PanelTransition.Show(Panel_PlayerList);
        });

        UIButtons.Bind(btn_quit, () =>
        {
            if (Panel_Quit != null) PanelTransition.Show(Panel_Quit);
            else QuitPopup.QuitGame();
        });
        UIButtons.Bind(btn_help, () => OpenSubPanel(Panel_Help));
        UIButtons.Bind(btn_option, () => OpenSubPanel(Panel_Option));

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.Disconnected += OnDisconnected;

            // 게임을 마치고 돌아왔으면 아직 방 안이므로 Room 씬으로
            if (NetworkManager.Instance.CurrentRoom != null) GoToRoomScene();
        }
    }

    // 옵션이나 접속자 목록이 떠 있으면 채팅 미리보기를 숨김 (어느 길로 닫혀도 다시 보이게 매 프레임 확인)
    private void Update()
    {
        if (chatPreview == null) return;
        chatPreview.SetBlocked(PanelTransition.IsShown(Panel_Option) || PanelTransition.IsShown(Panel_PlayerList));
    }

    protected override void OnDestroy()
    {
        if (NetworkManager.Instance != null) NetworkManager.Instance.Disconnected -= OnDisconnected;
        base.OnDestroy();
    }

    // ── 패널이 부르는 이동 ──────────────────────────────────────

    public void ShowMainMenu() => GoTo(Panel_MainMenu);
    public void ShowCreateRoom() => GoTo(Panel_CreateRoom);

    // 방을 만들거나 들어간 뒤
    public void GoToRoomScene()
    {
        LoadScene(m_roomSceneName, "Room 씬 이름(m_roomSceneName)");
    }

    // 로그아웃: 방에서 나가고 접속을 끊은 뒤 로그인 씬으로
    public void Logout()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected) NetworkManager.Instance.SignOut();
        LoadScene(m_loginSceneName, "로그인 씬 이름(m_loginSceneName)");
    }

    // 옵션·도움말 닫기: 열기 전 화면으로
    public void CloseSubPanel()
    {
        ShowOnly(returnPanels.Count > 0 ? returnPanels.Pop() : Panel_MainMenu);
    }

    // 메뉴 → 방 만들기처럼 앞으로 가는 이동. 돌아갈 곳 기록을 지움.
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

    private GameObject[] Panels => new[] { Panel_MainMenu, Panel_Option, Panel_Help, Panel_CreateRoom };

    // 하나만 켬. 켜던 패널은 사라지고 새 패널이 나타남 (PanelTransition)
    private void ShowOnly(GameObject panel)
    {
        currentPanel = panel;
        foreach (GameObject p in Panels)
        {
            if (p == null) continue;
            if (p == panel) PanelTransition.Show(p);
            else PanelTransition.Hide(p);
        }
    }

    // 접속이 끊기면(로그아웃 포함) 로그인 씬으로
    private void OnDisconnected()
    {
        LoadScene(m_loginSceneName, "로그인 씬 이름(m_loginSceneName)");
    }

    private void LoadScene(string sceneName, string what)
    {
        if (isLeavingScene) return;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"UIManager_Lobby: {what}을 넣어 주세요.", this);
            return;
        }

        isLeavingScene = true;
        SceneLoader.StartLoad(sceneName);
    }
}
