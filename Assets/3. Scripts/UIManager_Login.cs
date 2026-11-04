using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// 로그인 씬의 패널을 켜고 끄기만 함. 각 패널의 일은 패널에 붙은 스크립트가 함.
//   Panel_Title    (Login_TitlePanel)    로고 연출, Touch To Start, 네트워크 확인 → 로그인 창
//   Panel_Login    (Login_LoginPanel)    이메일 로그인 → 로비
//   Panel_SignUp   (Login_SignUpPanel)   이메일·비밀번호 확인 → 닉네임 창
//   Panel_Nickname (Login_NicknamePanel) 닉네임 정하고 가입 → 로비
//   Panel_Quit     (QuitPopup)           종료 확인. 위 패널들과 따로, 지금 화면 위에 겹쳐 뜸
public class UIManager_Login : Singleton<UIManager_Login>
{
    [Header("Panels")]
    [SerializeField] private GameObject panel_Title;
    [FormerlySerializedAs("panel_GuestLogin")]
    [SerializeField] private GameObject panel_Login;
    [SerializeField] private GameObject panel_SignUp;
    [SerializeField] private GameObject panel_Nickname;

    [Header("Quit")]
    [Tooltip("게임 종료 버튼. 누르면 종료 확인 팝업을 띄움")]
    [SerializeField] private Button btn_quit;
    [Tooltip("종료 확인 팝업 (QuitPopup이 붙은 오브젝트). 지금 패널 위에 겹쳐 뜸")]
    [SerializeField] private GameObject panel_Quit;

    [Header("Scene Settings")]
    [Tooltip("로그인 뒤 갈 로비 씬")]
    [SerializeField] private string m_lobbySceneName = "1. Scenes/2. Robby";

    private bool isLeaving;

    private void Start()
    {
        if (Instance != this) return;
        PanelTransition.SetImmediate(panel_Quit, false);
        UIButtons.Bind(btn_quit, ShowQuit);

        // 처음에는 타이틀만: 다른 패널은 바로 끄고 타이틀은 부드럽게 나타남
        foreach (GameObject panel in Panels)
        {
            if (panel != panel_Title) PanelTransition.SetImmediate(panel, false);
        }
        ShowTitle();
    }

    // 종료 확인 팝업. 없으면 바로 종료
    public void ShowQuit()
    {
        if (isLeaving) return;
        SoundManager.Play(SoundEffect.Click);
        if (panel_Quit != null) PanelTransition.Show(panel_Quit);
        else QuitPopup.QuitGame();
    }

    public void ShowTitle() => Show(panel_Title);
    public void ShowLogin() => Show(panel_Login);
    public void ShowSignUp() => Show(panel_SignUp);
    public void ShowNickname() => Show(panel_Nickname);

    private GameObject[] Panels => new[] { panel_Title, panel_Login, panel_SignUp, panel_Nickname };

    // 하나만 켬. 켜던 패널은 사라지고 새 패널이 나타남 (PanelTransition)
    private void Show(GameObject target)
    {
        if (isLeaving) return;
        if (target == null)
        {
            Debug.LogError("UIManager_Login: 켤 패널이 연결되지 않았습니다.", this);
            return;
        }

        foreach (GameObject panel in Panels)
        {
            if (panel != null && panel != target) PanelTransition.Hide(panel);
        }
        PanelTransition.Show(target);
    }

    // 로그인·가입이 성공하면 패널이 부름
    public void GoToLobby()
    {
        if (isLeaving) return;
        if (string.IsNullOrWhiteSpace(m_lobbySceneName))
        {
            Debug.LogError("UIManager_Login: 로비 씬 이름(m_lobbySceneName)을 넣어 주세요.", this);
            return;
        }

        isLeaving = true;
        SceneLoader.StartLoad(m_lobbySceneName);
    }
}
