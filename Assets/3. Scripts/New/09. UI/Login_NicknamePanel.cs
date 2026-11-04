using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 닉네임 생성 창. 회원가입 창에서 이메일·비밀번호를 확인한 뒤 열림.
// 결정을 누르면 그때 실제로 가입함 (가입 도중에 닫아도 닉네임 없는 계정이 남지 않게).
// 비워 두면 안내 글자의 기본 닉네임(성격 + 색깔 + 슬라임)을 씀.
// 이메일·비밀번호 문제(이미 가입된 이메일 등)면 회원가입 창으로 돌아가서 그 칸 옆에 이유를 보여 줌.
public class Login_NicknamePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField input_nickname;
    [SerializeField] private TMP_Text txt_nicknameError;
    [SerializeField] private Button btn_confirm;
    [Tooltip("회원가입 창으로 돌아가는 버튼 (X). 입력한 이메일·비밀번호는 그대로 남음")]
    [SerializeField] private Button btn_back;

    private Login_SignUpPanel signUp;   // 이메일·비밀번호를 넘겨준 회원가입 창
    private string defaultNickname;
    private bool isBusy;

    private void Awake()
    {
        UIButtons.Bind(btn_confirm, Confirm);
        UIButtons.Bind(btn_back, Back);
        if (input_nickname != null)
        {
            input_nickname.characterLimit = GameRules.NicknameMaxLength;
            input_nickname.onValueChanged.AddListener(_ => ShowError(null));
            input_nickname.onSubmit.AddListener(_ => Confirm());   // 엔터로도 결정
        }
    }

    // 회원가입 창이 부름
    public void Open(Login_SignUpPanel from)
    {
        signUp = from;
        if (UIManager_Login.Instance != null) UIManager_Login.Instance.ShowNickname();
    }

    private void OnEnable()
    {
        NetworkManager network = NetworkManager.Instance;
        defaultNickname = network != null ? network.SuggestNickname() : string.Empty;
        if (input_nickname != null)
        {
            input_nickname.text = string.Empty;
            if (input_nickname.placeholder is TMP_Text placeholder) placeholder.text = defaultNickname;
        }
        ShowError(null);

        if (network != null) network.ErrorOccurred += OnError;
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null) NetworkManager.Instance.ErrorOccurred -= OnError;
        isBusy = false;
        SetButtons(true);
    }

    private void Confirm()
    {
        if (isBusy) return;
        NetworkManager network = NetworkManager.Instance;
        if (network == null || signUp == null)
        {
            ShowError("네트워크를 시작할 수 없습니다.");
            return;
        }

        string typed = input_nickname != null ? input_nickname.text.Trim() : string.Empty;
        string nickname = typed.Length > 0 ? typed : defaultNickname;
        string problem = GameRules.CheckNickname(nickname);
        if (problem != null)
        {
            ShowError(problem);
            return;
        }

        isBusy = true;
        SetButtons(false);
        ShowError("가입 중...");
        network.SignUpWithEmail(signUp.Email, signUp.Password, nickname, Finish);
    }

    // 실패면 이유는 OnError가 이미 보여 줌
    private void Finish(UserInfo user)
    {
        if (this == null) return;   // 응답이 오기 전에 씬이 바뀜
        isBusy = false;
        SetButtons(true);
        if (user == null) return;

        ShowError("가입 완료! 로비로 이동합니다.");
        if (UIManager_Login.Instance != null) UIManager_Login.Instance.GoToLobby();
    }

    private void Back()
    {
        if (isBusy) return;
        SoundManager.Play(SoundEffect.Click);
        if (signUp != null) signUp.ReturnFromNickname(null);
    }

    private void OnError(string message)
    {
        // 이메일·비밀번호 문제는 회원가입 창에서 고쳐야 하므로 돌아감
        if (signUp != null && (message.Contains("이메일") || message.Contains("비밀번호")))
        {
            signUp.ReturnFromNickname(message);
            return;
        }
        ShowError(message);
    }

    private void ShowError(string message)
    {
        if (txt_nicknameError != null) txt_nicknameError.text = message ?? string.Empty;
    }

    private void SetButtons(bool interactable)
    {
        UIButtons.SetInteractable(btn_confirm, interactable);
        UIButtons.SetInteractable(btn_back, interactable);
    }
}
