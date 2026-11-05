using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로그인 창 (과제의 Login Scene: 이메일/비밀번호 로그인, 성공·실패 처리). 회원가입은 Login_SignUpPanel.
// 성공하면 UIManager_Login이 로비로 보냄. 실패하면 이유(비밀번호가 틀림 등)를 txt_status에 보여 줌.
// 이메일은 다음 접속 때 미리 채우고, 비밀번호는 저장하지 않음.
public class Login_LoginPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField input_email;
    [SerializeField] private TMP_InputField input_password;
    [SerializeField] private Button btn_login;
    [Tooltip("회원가입 창을 여는 버튼")]
    [SerializeField] private Button btn_openSignUp;
    [Tooltip("타이틀로 돌아가는 버튼 (X)")]
    [SerializeField] private Button btn_close;
    [Tooltip("성공·실패 안내 글자")]
    [SerializeField] private TMP_Text txt_status;
    [SerializeField] private Color errorColor = new Color(0.85f, 0.2f, 0.2f);
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f);

    private bool isBusy;

    private void Awake()
    {
        Login_AuthInput.HidePassword(input_password);
        UIButtons.Bind(btn_login, Login);
        UIButtons.Bind(btn_openSignUp, OpenSignUp);
        UIButtons.Bind(btn_close, Close);
        if (input_password != null) input_password.onSubmit.AddListener(_ => Login());   // 엔터로도 로그인
    }

    private void OnEnable()
    {
        NetworkManager network = NetworkManager.Instance;
        if (input_email != null && network != null && string.IsNullOrEmpty(input_email.text)) input_email.text = network.SavedEmail;
        if (input_password != null) input_password.text = string.Empty;
        ShowStatus(string.Empty, false);

        if (network != null) network.ErrorOccurred += OnError;
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null) NetworkManager.Instance.ErrorOccurred -= OnError;
        isBusy = false;
        SetButtons(true);
    }

    private void Login()
    {
        if (isBusy) return;
        NetworkManager network = NetworkManager.Instance;
        if (network == null)
        {
            ShowStatus("네트워크를 시작할 수 없습니다.", true);
            return;
        }

        isBusy = true;
        SetButtons(false);
        ShowStatus("로그인 중...", false);
        network.SignInWithEmail(Login_AuthInput.Text(input_email), Login_AuthInput.Text(input_password), Finish);
    }

    // 실패면 이유는 OnError가 이미 보여 줌
    private void Finish(UserInfo user)
    {
        if (this == null) return;   // 응답이 오기 전에 씬이 바뀜
        isBusy = false;
        SetButtons(true);
        if (user == null) return;

        ShowStatus("로그인 성공!", false);
        if (UIManager_Login.Instance != null) UIManager_Login.Instance.GoToLobby();
    }

    private void OpenSignUp()
    {
        if (isBusy) return;
        SoundManager.Play(SoundEffect.Click);
        if (UIManager_Login.Instance != null) UIManager_Login.Instance.ShowSignUp();
    }

    private void Close()
    {
        if (isBusy) return;
        SoundManager.Play(SoundEffect.Click);
        if (UIManager_Login.Instance != null) UIManager_Login.Instance.ShowTitle();
    }

    private void OnError(string message)
    {
        ShowStatus(message, true);
    }

    private void SetButtons(bool interactable)
    {
        UIButtons.SetInteractable(btn_login, interactable);
        UIButtons.SetInteractable(btn_openSignUp, interactable);
        UIButtons.SetInteractable(btn_close, interactable);
    }

    private void ShowStatus(string message, bool isError)
    {
        if (txt_status == null) return;
        txt_status.text = message;
        txt_status.color = isError ? errorColor : normalColor;
    }
}
