using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class UIManager_Start : Singleton<UIManager_Start>
{
    [Header("Title Letter Slam")]
    [Tooltip("S, L, I, M, E, P, O, P, !, !, ! 순서. 마지막 느낌표 3개는 동시에 등장합니다.")]
    [SerializeField] private RectTransform[] titleLetters = new RectTransform[0];
    [SerializeField, Min(0f)] private float letterInterval = 0.12f;
    [SerializeField, Min(0.05f)] private float letterDuration = 0.24f;
    [SerializeField, Min(1f)] private float letterStartScale = 3f;
    [SerializeField] private float letterStartHeight = 120f;
    [SerializeField, Min(0f)] private float slimeToPopDelay = 0.3f;
    [SerializeField, Min(0f)] private float popToExclamationDelay = 0.35f;
    [SerializeField, Min(0f)] private float titleHoldDuration = 0.4f;

    [Header("TTS (Tap To Start) Settings")]
    [FormerlySerializedAs("img_GameStart")]
    [Tooltip("페이드와 깜빡임을 적용할 Touch To Start 글자 이미지입니다.")]
    [SerializeField] private Image img_TTS;
    [FormerlySerializedAs("btn_GameStart")]
    [SerializeField] private Button btn_TTS;
    [SerializeField, Min(0f)] private float ttsFadeInDuration = 0.6f;
    [SerializeField, Range(0f, 1f)] private float ttsMinAlpha = 0.25f;
    [SerializeField, Min(0.05f)] private float ttsPulseDuration = 0.8f;

    [Header("Title / Login Panels")]
    [SerializeField] private GameObject panel_Title;
    [Tooltip("LoginPopUp만이 아니라 결정 버튼까지 포함한 LoginPanel 전체를 연결합니다.")]
    [SerializeField] private GameObject panel_GuestLogin;

    [Header("Guest Login")]
    [Tooltip("선택 사항. 닫기 버튼이 있으면 타이틀로 돌아갑니다.")]
    [SerializeField] private Button btn_GuestLoginClose;
    [Tooltip("Txt_Input 텍스트가 아니라 InputField (TMP) 컴포넌트를 연결합니다.")]
    [SerializeField] private TMP_InputField input_Nickname;
    [SerializeField] private Button btn_GuestLoginConfirm;
    [SerializeField] private TMP_Text txt_GuestUid;

    [Header("Connection Status")]
    [SerializeField] private TMP_Text txt_StartStatus;

    [Header("Scene Settings")]
    [SerializeField] private string m_sceneName = "2. GameMenu";

    private Sequence introTween;
    private Tween ttsPulseTween;
    private LetterPose[] letterPoses;
    private NetworkManager network;
    private StartState startState = StartState.Intro;
    private string defaultNickname;
    private int reservedGuestNumber;
    private int requestVersion;
    private bool initialized;

    private enum StartState { Intro, Ready, CheckingNetwork, GuestLogin, SigningIn, MovingToMenu }

    private struct LetterPose
    {
        public RectTransform target;
        public Vector2 position;
        public Vector3 scale;
        public Quaternion rotation;

        public void Restore()
        {
            if (target == null) return;
            target.anchoredPosition = position;
            target.localScale = scale;
            target.localRotation = rotation;
        }
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        BindButtons(true);
        BindNetworkEvents();
        if (!initialized) return;
        if (startState == StartState.Intro) ReplayIntro();
        else if (startState != StartState.MovingToMenu) ReturnToReady();
    }

    private void Start()
    {
        if (Instance != this) return;
        initialized = true;
        BindButtons(true);
        BindNetworkEvents();
        SetLoginPanelVisible(false);
        SetStartStatus(string.Empty);
        ReplayIntro();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            ReplayIntro();
    }
#endif

    private void BindButtons(bool subscribe)
    {
        if (btn_TTS != null)
        {
            btn_TTS.onClick.RemoveListener(OnTouchToStartClicked);
            if (subscribe) btn_TTS.onClick.AddListener(OnTouchToStartClicked);
        }
        if (btn_GuestLoginClose != null)
        {
            btn_GuestLoginClose.onClick.RemoveListener(CloseGuestLoginPopup);
            if (subscribe) btn_GuestLoginClose.onClick.AddListener(CloseGuestLoginPopup);
        }
        if (btn_GuestLoginConfirm != null)
        {
            btn_GuestLoginConfirm.onClick.RemoveListener(SubmitGuestLogin);
            if (subscribe) btn_GuestLoginConfirm.onClick.AddListener(SubmitGuestLogin);
        }
    }

    private void BindNetworkEvents()
    {
        NetworkManager current = NetworkManager.Instance;
        if (network == current) return;
        UnbindNetworkEvents();
        network = current;
        if (network != null) network.ErrorOccurred += HandleStartFlowError;
    }

    private void UnbindNetworkEvents()
    {
        if (network != null) network.ErrorOccurred -= HandleStartFlowError;
        network = null;
    }

    public void OnTouchToStartClicked()
    {
        if (!isActiveAndEnabled || startState != StartState.Ready) return;
        SoundManager.Play(SoundEffect.Click);
        BindNetworkEvents();
        startState = StartState.CheckingNetwork;
        if (btn_TTS != null) btn_TTS.interactable = false;
        PauseStartPrompt();
        SetStartStatus("네트워크 연결 확인 중…");
        if (network == null)
        {
            HandleStartFlowError("네트워크를 시작할 수 없습니다.");
            return;
        }

        int version = ++requestVersion;
        network.CheckConnection(connected =>
        {
            if (CanHandle(version, StartState.CheckingNetwork))
                HandleNetworkCheckResult(connected);
        });
    }

    public void HandleNetworkCheckResult(bool isConnected)
    {
        if (!isActiveAndEnabled || startState != StartState.CheckingNetwork) return;
        if (!isConnected || network == null)
        {
            HandleStartFlowError("네트워크에 연결할 수 없습니다. 연결 상태를 확인하고 다시 시도해 주세요.");
            return;
        }
        OpenGuestLoginPanel();
    }

    private void OpenGuestLoginPanel()
    {
        if (panel_Title == null || panel_GuestLogin == null || panel_Title == panel_GuestLogin ||
            input_Nickname == null || !(input_Nickname.placeholder is TMP_Text) ||
            btn_GuestLoginConfirm == null || txt_GuestUid == null)
        {
            HandleStartFlowError("타이틀·로그인 패널, 닉네임 입력창, 결정 버튼, UID 텍스트 연결을 확인해 주세요.");
            return;
        }

        defaultNickname = network.SuggestNickname();
        if (string.IsNullOrWhiteSpace(defaultNickname))
        {
            HandleStartFlowError("기본 닉네임을 가져오지 못했습니다. 다시 시도해 주세요.");
            return;
        }
        defaultNickname = defaultNickname.Trim();

        // 기본 이름은 실제 입력값이 아니라 Placeholder에만 표시합니다.
        var placeholder = (TMP_Text)input_Nickname.placeholder;
        placeholder.richText = false;
        placeholder.text = defaultNickname;
        input_Nickname.SetTextWithoutNotify(string.Empty);

        reservedGuestNumber = 0;
        startState = StartState.GuestLogin;
        SetStartStatus(string.Empty);
        txt_GuestUid.text = "UID 발급 중…";
        UpdateLoginControls();
        SetLoginPanelVisible(true);

        // 화면에 표시한 번호가 그대로 로그인에 쓰이도록 기존 API에서 먼저 예약합니다.
        int version = requestVersion;
        network.ReserveGuestNumber(number =>
        {
            if (CanHandle(version, StartState.GuestLogin))
                HandleGuestNumberReserved(number);
        });
    }

    private void HandleGuestNumberReserved(int guestNumber)
    {
        if (!isActiveAndEnabled || startState != StartState.GuestLogin) return;
        if (guestNumber <= 0)
        {
            HandleStartFlowError("UID를 받지 못했습니다. 다시 시도해 주세요.");
            return;
        }
        reservedGuestNumber = guestNumber;
        if (txt_GuestUid != null)
            txt_GuestUid.text = $"UID {UserInfo.FormatGuestNumber(guestNumber)}";
        UpdateLoginControls();
    }

    public void SubmitGuestLogin()
    {
        if (!isActiveAndEnabled || startState != StartState.GuestLogin || reservedGuestNumber <= 0) return;
        if (network == null)
        {
            HandleStartFlowError("네트워크를 시작할 수 없습니다.");
            return;
        }
        if (string.IsNullOrWhiteSpace(m_sceneName))
        {
            HandleStartFlowError("이동할 메뉴 씬을 연결해 주세요.");
            return;
        }

        string enteredNickname = input_Nickname != null ? input_Nickname.text : string.Empty;
        string nickname = string.IsNullOrWhiteSpace(enteredNickname)
            ? defaultNickname : enteredNickname.Trim();

        SoundManager.Play(SoundEffect.Click);
        startState = StartState.SigningIn;
        UpdateLoginControls();
        SetStartStatus("로그인 중…");
        int version = ++requestVersion;
        network.SignIn(nickname, user =>
        {
            if (CanHandle(version, StartState.SigningIn))
                HandleGuestLoginCompleted(user != null ? user.uid : null);
        });
    }

    public void CloseGuestLoginPopup()
    {
        if (!isActiveAndEnabled || startState != StartState.GuestLogin) return;
        SoundManager.Play(SoundEffect.Click);
        SetStartStatus(string.Empty);
        ReturnToReady();
    }

    public void HandleGuestLoginCompleted(string playerId)
    {
        if (!isActiveAndEnabled || startState != StartState.SigningIn) return;
        if (string.IsNullOrWhiteSpace(playerId))
        {
            HandleStartFlowError("로그인을 완료하지 못했습니다. 다시 시도해 주세요.");
            return;
        }
        MoveToMenu();
    }

    public void HandleStartFlowError(string message)
    {
        if (!isActiveAndEnabled || (startState != StartState.CheckingNetwork &&
            startState != StartState.GuestLogin && startState != StartState.SigningIn)) return;
        SetStartStatus(string.IsNullOrWhiteSpace(message)
            ? "연결을 확인하지 못했습니다. 다시 시도해 주세요." : message);
        if (startState == StartState.CheckingNetwork || reservedGuestNumber <= 0 || network == null)
        {
            ReturnToReady();
            return;
        }

        // 로그인 실패 시 같은 기본 이름과 예약 번호로 다시 시도합니다.
        ++requestVersion;
        startState = StartState.GuestLogin;
        UpdateLoginControls();
    }

    private bool CanHandle(int version, StartState state)
    {
        return isActiveAndEnabled && version == requestVersion && startState == state;
    }

    private void MoveToMenu()
    {
        if (string.IsNullOrWhiteSpace(m_sceneName))
        {
            HandleStartFlowError("이동할 메뉴 씬을 연결해 주세요.");
            return;
        }
        startState = StartState.MovingToMenu;
        UpdateLoginControls();
        SetStartStatus(string.Empty);
        SceneLoader.StartLoad(m_sceneName);
    }

    private void UpdateLoginControls()
    {
        bool canEdit = startState == StartState.GuestLogin;
        if (input_Nickname != null) input_Nickname.interactable = canEdit;
        if (btn_GuestLoginConfirm != null)
            btn_GuestLoginConfirm.interactable = canEdit && reservedGuestNumber > 0;
        if (btn_GuestLoginClose != null) btn_GuestLoginClose.interactable = canEdit;
    }

    private void SetLoginPanelVisible(bool visible)
    {
        if (panel_Title != null) panel_Title.SetActive(!visible);
        if (panel_GuestLogin != null) panel_GuestLogin.SetActive(visible);
    }

    private void ReturnToReady()
    {
        ++requestVersion;
        startState = StartState.Ready;
        SetLoginPanelVisible(false);
        if (btn_TTS != null) btn_TTS.interactable = true;
        StartTTSPulse();
    }

    private void PauseStartPrompt()
    {
        KillTTSTweens();
        SetTTSAlpha(1f);
    }

    private void SetStartStatus(string message)
    {
        if (txt_StartStatus != null) txt_StartStatus.text = message;
        else if (!string.IsNullOrEmpty(message)) Debug.Log(message, this);
    }

    [ContextMenu("Replay Title Intro")]
    public void ReplayIntro()
    {
        if (!isActiveAndEnabled || (startState != StartState.Intro && startState != StartState.Ready)) return;
        KillIntroTween();
        KillTTSTweens();
        RestoreTitleLetters();
        startState = StartState.Intro;
        SetLoginPanelVisible(false);
        SetStartStatus(string.Empty);
        if (btn_TTS != null) btn_TTS.interactable = false;
        if (img_TTS != null) img_TTS.enabled = true;
        SetTTSAlpha(0f);

        PrepareTitleLetters();
        introTween = DOTween.Sequence().SetUpdate(true);
        float duration = Mathf.Max(0.05f, letterDuration);
        float interval = Mathf.Max(0f, letterInterval);
        int letterIndex = 0;
        for (int i = 0; i < letterPoses.Length; i++)
        {
            LetterPose pose = letterPoses[i];
            if (pose.target == null) continue;
            int index = i;
            float startTime = TitleLetterStartTime(letterIndex++, interval, duration);
            Tween slam = DOTween.To(() => 0f, t => AnimateLetter(pose, index, t), 1f, duration)
                .SetEase(Ease.Linear);
            introTween.Insert(startTime, slam);
        }
        introTween.AppendInterval(Mathf.Max(0f, titleHoldDuration));
        if (img_TTS != null)
            introTween.Append(img_TTS.DOFade(1f, Mathf.Max(0f, ttsFadeInDuration)).SetEase(Ease.InOutSine));
        introTween.OnComplete(() =>
        {
            introTween = null;
            if (!isActiveAndEnabled || startState != StartState.Intro) return;
            RestoreTitleLetters();
            ReturnToReady();
        });
    }

    private void PrepareTitleLetters()
    {
        letterPoses = new LetterPose[titleLetters != null ? titleLetters.Length : 0];
        for (int i = 0; i < letterPoses.Length; i++)
        {
            RectTransform target = titleLetters[i];
            if (target == null) continue;
            letterPoses[i] = new LetterPose
            {
                target = target,
                position = target.anchoredPosition,
                scale = target.localScale,
                rotation = target.localRotation
            };
            target.localScale = Vector3.zero;
        }
    }

    private void AnimateLetter(LetterPose pose, int index, float t)
    {
        if (pose.target == null) return;
        if (t >= 1f) { pose.Restore(); return; }
        if (t < 0.65f)
        {
            float approach = 1f - Mathf.Pow(1f - t / 0.65f, 3f);
            Vector3 scale = Vector3.Lerp(Vector3.one * Mathf.Max(1f, letterStartScale),
                new Vector3(1.12f, 0.84f, 1f), approach);
            pose.target.localScale = Vector3.Scale(pose.scale, scale);
            pose.target.anchoredPosition = pose.position + Vector2.up * (letterStartHeight * (1f - approach));
            float tilt = (index % 2 == 0 ? -5f : 5f) * (1f - approach);
            pose.target.localRotation = pose.rotation * Quaternion.Euler(0f, 0f, tilt);
        }
        else
        {
            float settle = (t - 0.65f) / 0.35f;
            Vector3 squash = new Vector3(1.12f, 0.84f, 1f);
            Vector3 rebound = new Vector3(0.96f, 1.06f, 1f);
            Vector3 scale = settle < 0.5f
                ? Vector3.Lerp(squash, rebound, Mathf.SmoothStep(0f, 1f, settle * 2f))
                : Vector3.Lerp(rebound, Vector3.one, Mathf.SmoothStep(0f, 1f, (settle - 0.5f) * 2f));
            pose.target.localScale = Vector3.Scale(pose.scale, scale);
            pose.target.anchoredPosition = pose.position;
            pose.target.localRotation = pose.rotation;
        }
    }

    private float TitleLetterStartTime(int letterIndex, float interval, float duration)
    {
        if (letterIndex < 5) return letterIndex * interval;
        float popStart = 4 * interval + duration + Mathf.Max(0f, slimeToPopDelay);
        if (letterIndex < 8) return popStart + (letterIndex - 5) * interval;
        return popStart + 2 * interval + duration + Mathf.Max(0f, popToExclamationDelay);
    }

    private void RestoreTitleLetters()
    {
        if (letterPoses != null)
            foreach (LetterPose pose in letterPoses) pose.Restore();
    }

    private void StartTTSPulse()
    {
        if (img_TTS == null) return;
        KillTTSTweens();
        img_TTS.enabled = true;
        SetTTSAlpha(1f);
        ttsPulseTween = img_TTS.DOFade(Mathf.Clamp01(ttsMinAlpha), Mathf.Max(0.05f, ttsPulseDuration))
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
    }

    private void KillIntroTween()
    {
        introTween?.Kill();
        introTween = null;
    }

    private void KillTTSTweens()
    {
        ttsPulseTween?.Kill();
        ttsPulseTween = null;
    }

    private void SetTTSAlpha(float alpha)
    {
        if (img_TTS == null) return;
        Color color = img_TTS.color;
        color.a = alpha;
        img_TTS.color = color;
    }

    public void StopTwinkle()
    {
        KillTTSTweens();
        SetTTSAlpha(0f);
    }

    private void OnDisable()
    {
        ++requestVersion;
        BindButtons(false);
        UnbindNetworkEvents();
        KillIntroTween();
        KillTTSTweens();
        RestoreTitleLetters();
        SetTTSAlpha(0f);
        if (btn_TTS != null) btn_TTS.interactable = false;
    }

    protected override void OnDestroy()
    {
        BindButtons(false);
        UnbindNetworkEvents();
        KillIntroTween();
        KillTTSTweens();
        base.OnDestroy();
    }
}
