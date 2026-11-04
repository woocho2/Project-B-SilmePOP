using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 룸 씬 (과제의 Room Scene). 화면 위 버튼(종료·옵션)과 겹쳐 뜨는 팝업, 씬 이동만 맡음.
// 방 정보·참가자 카드·나가기 버튼은 RoomPanel, 방 채팅은 ChatPanel(Channel = Room)이 함.
// 씬 이동: 방에서 빠지면(나가기, 방이 없어짐) 로비, 접속이 끊기면 로그인, 방장이 시작하면 5초 세고 게임.
public class UIManager_Room : Singleton<UIManager_Room>
{
    [Header("화면 위 버튼")]
    [SerializeField] private Button btn_quit;     // QuitPanel 띄움
    [SerializeField] private Button btn_option;   // OptionPanel 띄움

    [Header("겹쳐 뜨는 팝업 (선택 사항)")]
    [Tooltip("종료 확인 팝업 (QuitPopup). 없으면 종료 버튼이 바로 종료함")]
    [SerializeField] private GameObject Panel_Quit;
    [Tooltip("옵션 팝업 (OptionPanel). 닫기를 누르면 팝업만 닫힘")]
    [SerializeField] private GameObject Panel_Option;

    [Header("Scene Settings")]
    [Tooltip("방에서 나오면 이 씬으로")]
    [SerializeField] private string m_lobbySceneName = "1. Scenes/2. Robby";
    [Tooltip("방장이 시작하면 모두 이 씬으로")]
    [SerializeField] private string m_gameSceneName = "1. Scenes/4. Game";
    [Tooltip("접속이 끊기면 이 씬으로")]
    [SerializeField] private string m_loginSceneName = "1. Scenes/1. Login";

    private Sequence startCountdown;
    private bool isLeavingScene;

    private void Start()
    {
        if (Instance != this) return;

        PanelTransition.SetImmediate(Panel_Quit, false);
        PanelTransition.SetImmediate(Panel_Option, false);

        UIButtons.Bind(btn_quit, () =>
        {
            if (Panel_Quit != null) PanelTransition.Show(Panel_Quit);
            else QuitPopup.QuitGame();
        });
        UIButtons.Bind(btn_option, () => PanelTransition.Show(Panel_Option));

        NetworkManager network = NetworkManager.Instance;
        if (network == null) return;

        network.RoomChanged += OnRoomChanged;
        network.MatchStarting += OnMatchStarting;
        network.Disconnected += OnDisconnected;

        if (network.CurrentRoom == null) LoadScene(m_lobbySceneName, "로비 씬 이름(m_lobbySceneName)");   // 방 없이 열림
    }

    protected override void OnDestroy()
    {
        startCountdown?.Kill();
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.RoomChanged -= OnRoomChanged;
            NetworkManager.Instance.MatchStarting -= OnMatchStarting;
            NetworkManager.Instance.Disconnected -= OnDisconnected;
        }
        base.OnDestroy();
    }

    // 방에서 나왔으면 (나가기, 방이 없어짐, 끊겨서 빠짐) 로비로. 세는 중이었으면 게임으로 넘어가지 않음.
    private void OnRoomChanged(RoomInfo room)
    {
        if (room != null) return;

        startCountdown?.Kill();
        LoadScene(m_lobbySceneName, "로비 씬 이름(m_lobbySceneName)");
    }

    private void OnDisconnected()
    {
        startCountdown?.Kill();
        LoadScene(m_loginSceneName, "로그인 씬 이름(m_loginSceneName)");
    }

    // 방장이 시작함: 방 채팅에 5, 4, 3, 2, 1을 띄운 뒤 모든 참가자가 게임 씬으로.
    // 서버 시각(startAt) 기준으로 세므로 모든 화면에서 같은 순간에 바뀜.
    private void OnMatchStarting(MatchSettings settings)
    {
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
            LoadScene(m_gameSceneName, "게임 씬 이름(m_gameSceneName)");
        });
    }

    private void LoadScene(string sceneName, string what)
    {
        if (isLeavingScene) return;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"UIManager_Room: {what}을 넣어 주세요.", this);
            return;
        }

        isLeavingScene = true;
        SceneLoader.StartLoad(sceneName);
    }
}
