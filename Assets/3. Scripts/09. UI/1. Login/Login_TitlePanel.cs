using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 패널: SLIMEPOP!!! 글자 등장 연출 → Touch To Start 깜빡임 → 누르면 네트워크 확인 → 로그인 창.
// 처음 켜질 때만 글자 연출을 하고, 로그인 창에서 닫고 돌아오면 바로 Touch To Start부터.
public class Login_TitlePanel : MonoBehaviour
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
    [Tooltip("타이틀 화면이 나타난 뒤 글자가 떨어지기 시작할 때까지")]
    [SerializeField, Min(0f)] private float introStartDelay = 0.3f;
    private const float MaxIntroStep = 1f / 30f;

    [Header("TTS (Tap To Start)")]
    [Tooltip("페이드와 깜빡임을 적용할 Touch To Start 글자 이미지")]
    [SerializeField] private Image img_TTS;
    [SerializeField] private Button btn_TTS;
    [SerializeField, Min(0f)] private float ttsFadeInDuration = 0.6f;
    [SerializeField, Range(0f, 1f)] private float ttsMinAlpha = 0.25f;
    [SerializeField, Min(0.05f)] private float ttsPulseDuration = 0.8f;

    [Header("Connection Status")]
    [Tooltip("\"네트워크 연결 확인 중…\"과 연결 실패 이유")]
    [SerializeField] private TMP_Text txt_status;

    private Sequence introTween;
    private Tween ttsPulseTween;
    private LetterPose[] letterPoses;
    private TitleState state = TitleState.Intro;
    private bool introPlayed;
    private int requestVersion;   // 패널이 꺼진 뒤 늦게 온 네트워크 응답은 버림

    private enum TitleState { Intro, Ready, CheckingNetwork }

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

    private void Awake()
    {
        UIButtons.Bind(btn_TTS, OnTouchToStart);
    }

    // 글자 연출은 한 프레임에 최대 MaxIntroStep초만 진행 (멈칫한 프레임에서 몇 글자를 한꺼번에 건너뛰지 않게)
    private void Update()
    {
        if (introTween == null) return;
        float step = Mathf.Min(Time.unscaledDeltaTime, MaxIntroStep);
        DOTween.ManualUpdate(step, step);
    }

    private void OnEnable()
    {
        SetStatus(string.Empty);
        if (introPlayed) ReturnToReady();
        else ReplayIntro();
    }

    private void OnDisable()
    {
        ++requestVersion;
        KillIntroTween();
        KillTTSTween();
        RestoreTitleLetters();
        if (state == TitleState.Intro) introPlayed = false;   // 연출 도중에 꺼졌으면 다음에 다시
    }

    private void OnTouchToStart()
    {
        if (state != TitleState.Ready) return;
        NetworkManager network = NetworkManager.Instance;
        SoundManager.Play(SoundEffect.Click);
        if (network == null)
        {
            SetStatus("네트워크를 시작할 수 없습니다.");
            return;
        }

        state = TitleState.CheckingNetwork;
        UIButtons.SetInteractable(btn_TTS, false);
        KillTTSTween();
        SetTTSAlpha(1f);
        SetStatus("네트워크 연결 확인 중…");

        int version = ++requestVersion;
        network.CheckConnection(connected =>
        {
            if (this == null || version != requestVersion) return;
            if (!connected)
            {
                SetStatus("네트워크에 연결할 수 없습니다. 연결 상태를 확인하고 다시 시도해 주세요.");
                ReturnToReady();
                return;
            }
            SetStatus(string.Empty);
            if (UIManager_Login.Instance != null) UIManager_Login.Instance.ShowLogin();
        });
    }

    private void ReturnToReady()
    {
        state = TitleState.Ready;
        UIButtons.SetInteractable(btn_TTS, true);
        StartTTSPulse();
    }

    private void SetStatus(string message)
    {
        if (txt_status != null) txt_status.text = message;
    }

    // ── 글자 등장 연출 ──────────────────────────────────────────

    [ContextMenu("Replay Title Intro")]
    public void ReplayIntro()
    {
        if (!isActiveAndEnabled || state == TitleState.CheckingNetwork) return;
        KillIntroTween();
        KillTTSTween();
        RestoreTitleLetters();
        state = TitleState.Intro;
        introPlayed = true;
        UIButtons.SetInteractable(btn_TTS, false);
        if (img_TTS != null) img_TTS.enabled = true;
        SetTTSAlpha(0f);

        PrepareTitleLetters();
        // 직접 시간을 넣어 돌림(Update): 게임을 켠 직후 한 프레임이 길게 걸려도 연출을 건너뛰지 않게
        introTween = DOTween.Sequence().SetUpdate(UpdateType.Manual, true);
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
        introTween.PrependInterval(Mathf.Max(0f, introStartDelay));   // 패널이 다 나타난 뒤 시작
        introTween.OnComplete(() =>
        {
            introTween = null;
            if (!isActiveAndEnabled || state != TitleState.Intro) return;
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

    // SLIME(0~4) → 쉬고 → POP(5~7) → 쉬고 → !!!(8~) 동시에
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

    // ── Touch To Start 깜빡임 ───────────────────────────────────

    private void StartTTSPulse()
    {
        if (img_TTS == null) return;
        KillTTSTween();
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

    private void KillTTSTween()
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
}
