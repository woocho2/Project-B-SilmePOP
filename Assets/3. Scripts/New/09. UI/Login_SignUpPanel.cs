using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 회원가입 창 (과제의 Login Scene: 회원가입). 이메일·비밀번호를 확인하고 닉네임 생성 창(Login_NicknamePanel)으로 넘김.
// 실제 가입은 닉네임 창에서 결정을 누를 때 함. 가입하면 바로 로그인돼서 로비로 감.
// 실패 이유는 칸마다 옆에 보여 주고, 그 칸을 다시 고치면 지움.
public class Login_SignUpPanel : MonoBehaviour
{
    private const int MinPasswordLength = 6;   // Firebase 규칙

    [SerializeField] private TMP_InputField input_email;
    [SerializeField] private TMP_InputField input_password;
    [Tooltip("비밀번호 확인 칸")]
    [SerializeField] private TMP_InputField input_passwordConfirm;
    [Tooltip("결정 버튼")]
    [SerializeField] private Button btn_signUp;
    [Tooltip("로그인 창으로 돌아가는 버튼 (X)")]
    [SerializeField] private Button btn_back;
    [Tooltip("다음에 열 닉네임 생성 창")]
    [SerializeField] private Login_NicknamePanel nicknamePanel;

    [Header("칸별 실패 이유")]
    [SerializeField] private TMP_Text txt_emailError;
    [SerializeField] private TMP_Text txt_passwordError;
    [SerializeField] private TMP_Text txt_passwordConfirmError;

    private bool keepInput;   // 닉네임 창에서 돌아옴: 입력한 것을 지우지 않음

    public string Email => Login_AuthInput.Text(input_email).Trim();
    public string Password => Login_AuthInput.Text(input_password);

    private void Awake()
    {
        Login_AuthInput.HidePassword(input_password);
        Login_AuthInput.HidePassword(input_passwordConfirm);
        UIButtons.Bind(btn_signUp, Next);
        UIButtons.Bind(btn_back, Back);

        ClearWhenEdited(input_email, txt_emailError);
        ClearWhenEdited(input_password, txt_passwordError);
        ClearWhenEdited(input_passwordConfirm, txt_passwordConfirmError);
    }

    private void OnEnable()
    {
        if (keepInput)
        {
            keepInput = false;
            return;
        }

        SetText(input_email, string.Empty);
        SetText(input_password, string.Empty);
        SetText(input_passwordConfirm, string.Empty);
        ClearErrors();
    }

    private void Next()
    {
        if (!CheckInput()) return;
        if (nicknamePanel == null)
        {
            Debug.LogError("Login_SignUpPanel: nicknamePanel을 연결해 주세요.", this);
            return;
        }

        SoundManager.Play(SoundEffect.Click);
        nicknamePanel.Open(this);
    }

    // 닉네임 창이 부름. error가 있으면 맞는 칸 옆에 보여 줌 (이미 가입된 이메일 등)
    public void ReturnFromNickname(string error)
    {
        keepInput = true;
        if (UIManager_Login.Instance != null) UIManager_Login.Instance.ShowSignUp();
        if (string.IsNullOrEmpty(error)) return;

        if (error.Contains("비밀번호")) ShowError(txt_passwordError, error);
        else ShowError(txt_emailError, error);
    }

    // 칸마다 확인해서 틀린 칸을 한 번에 다 보여 줌
    private bool CheckInput()
    {
        ClearErrors();
        string email = Email;
        string password = Password;

        string emailError = email.Length == 0 ? "이메일을 입력하세요."
                          : !email.Contains("@") || !email.Contains(".") ? "이메일 형식이 올바르지 않습니다."
                          : null;
        string passwordError = password.Length == 0 ? "비밀번호를 입력하세요."
                             : password.Length < MinPasswordLength ? $"비밀번호는 {MinPasswordLength}자 이상이어야 합니다."
                             : null;
        string confirmError = password != Login_AuthInput.Text(input_passwordConfirm) ? "비밀번호와 일치하지 않습니다." : null;

        ShowError(txt_emailError, emailError);
        ShowError(txt_passwordError, passwordError);
        ShowError(txt_passwordConfirmError, confirmError);
        return emailError == null && passwordError == null && confirmError == null;
    }

    private void Back()
    {
        SoundManager.Play(SoundEffect.Click);
        if (UIManager_Login.Instance != null) UIManager_Login.Instance.ShowLogin();
    }

    private void ClearErrors()
    {
        ShowError(txt_emailError, null);
        ShowError(txt_passwordError, null);
        ShowError(txt_passwordConfirmError, null);
    }

    private static void ShowError(TMP_Text text, string message)
    {
        if (text != null) text.text = message ?? string.Empty;
    }

    private static void ClearWhenEdited(TMP_InputField field, TMP_Text error)
    {
        if (field != null) field.onValueChanged.AddListener(_ => ShowError(error, null));
    }

    private static void SetText(TMP_InputField field, string value)
    {
        if (field != null) field.text = value;
    }
}
