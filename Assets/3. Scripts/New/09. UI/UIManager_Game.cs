using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 게임 화면: 시작 카운트다운, 타이머, 목표 합, 점수, 막혔을 때 안내, 게임오버 패널.
// 스킬 버튼과 단축키는 SkillManager가 맡음.
public class UIManager_Game : Singleton<UIManager_Game>
{
    [Header("Start Countdown")]
    [SerializeField] private TextMeshProUGUI txt_startCountdown;
    [SerializeField, Min(0.1f)] private float countdownInterval = 1f;
    [SerializeField, Min(0.1f)] private float gameStartDuration = 0.7f;
    private Sequence countdownTween;
    private bool isCountingDown;

    [Header("Game Panel")]
    [SerializeField] private GameObject Panel_Game;
    [SerializeField] private TextMeshProUGUI txt_timer;
    [SerializeField] private TextMeshProUGUI txt_score;
    [SerializeField] private TMP_Text txt_targetSum;
    [SerializeField] private Slider m_timerSlider;
    [SerializeField] private GameObject m_score;
    [SerializeField] private TextMeshProUGUI txt_warning;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject Panel_GameOver;
    [SerializeField] private GameObject m_gameoverscore;
    [SerializeField] private Button btn_GameOverRestart;
    [SerializeField] private Button btn_GameOverHome;
    [SerializeField] private TextMeshProUGUI txt_gameOverscore;

    [SerializeField] string m_sceneName;

    private bool isTimerRunning = false;

    private Sequence warningTween;
    private Vector2 warningHomePosition;

    private void Start()
    {
        InitTimerUI();
        InitScoreUI();
        InitUI();
        UIButtons.Bind(btn_GameOverRestart, RestartGame);
        UIButtons.Bind(btn_GameOverHome, () => SceneLoader.StartLoad(m_sceneName));

        if (txt_warning != null) warningHomePosition = txt_warning.rectTransform.anchoredPosition;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += GameOverUI;
            GameManager.Instance.TargetSumChanged += UpdateTargetSumUI;
            GameManager.Instance.NoMoreMoves += OnNoMoreMoves;
            UpdateTargetSumUI(GameManager.Instance.TargetSum);
        }
        BeginStartCountdown();
    }

    public void BeginStartCountdown()
    {
        countdownTween?.Kill();
        countdownTween = null;
        if (GameManager.Instance == null) return;

        if (txt_startCountdown == null)
        {
            Debug.LogWarning("Assign a TMP UI text to Start Countdown / Txt Start Countdown.", this);
            isCountingDown = false;
            GameManager.Instance.StartGameTimer();
            return;
        }

        isCountingDown = true;
        GameManager.Instance.PrepareStartCountdown();
        isTimerRunning = false;

        txt_startCountdown.raycastTarget = false;
        txt_startCountdown.gameObject.SetActive(true);
        if (GameManager.Instance.IsReadyToCountdown)
        {
            PlayStartCountdown();
        }
        else
        {
            // 방에서 시작한 판: 모든 참가자가 게임 씬에 올 때까지 기다림
            txt_startCountdown.text = "다른 플레이어를 기다리는 중...";
            NetworkManager.Instance.RoomChanged += OnRoomChangedWhileWaiting;
        }
    }

    private void OnRoomChangedWhileWaiting(RoomInfo room)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsReadyToCountdown) return;

        StopWaitingForPlayers();
        PlayStartCountdown();
    }

    private void StopWaitingForPlayers()
    {
        if (NetworkManager.Instance != null) NetworkManager.Instance.RoomChanged -= OnRoomChangedWhileWaiting;
    }

    private void PlayStartCountdown()
    {
        txt_startCountdown.text = "";

        countdownTween = DOTween.Sequence().SetUpdate(true);
        // 방에서 시작한 판이면 서버가 정한 시각(playAt)까지 기다려 모든 참가자가 함께 카운트다운함
        countdownTween.AppendInterval(GameManager.Instance.SecondsUntilStart);
        for (int count = GameRules.CountdownFrom; count >= 1; count--)
        {
            string text = count.ToString();
            countdownTween.AppendCallback(() =>
            {
                txt_startCountdown.text = text;
                SoundManager.Play(SoundEffect.Countdown);
            });
            countdownTween.AppendInterval(Mathf.Max(0.1f, countdownInterval));
        }

        countdownTween.AppendCallback(() =>
        {
            txt_startCountdown.text = "GAME START!!";
            SoundManager.Play(SoundEffect.GameStart);
        });
        countdownTween.AppendInterval(Mathf.Max(0.1f, gameStartDuration));
        countdownTween.OnComplete(() =>
        {
            txt_startCountdown.gameObject.SetActive(false);
            isCountingDown = false;
            countdownTween = null;
            if (GameManager.Instance != null) GameManager.Instance.StartGameTimer();
        });
    }

    private void OnDisable()
    {
        StopWaitingForPlayers();
        countdownTween?.Kill();
        countdownTween = null;
        if (txt_startCountdown != null) txt_startCountdown.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (isCountingDown) BeginStartCountdown();
    }

    private void InitTimerUI()
    {
        if (m_timerSlider != null)
        {
            m_timerSlider.minValue = 0f;
            m_timerSlider.maxValue = 1f;
            m_timerSlider.value = 0f;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimerUpdated += UpdateTimerUI;
            UpdateTimerUI(GameManager.Instance.CurrentTime);
            isTimerRunning = true;
        }
    }

    private void InitUI()
    {
        ShowPanel(Panel_Game);
        if (txt_warning != null) txt_warning.gameObject.SetActive(false);
    }

    // 게임 / 게임오버 패널 중 하나만 켬. 씬에 없는 패널은 건너뜀.
    private void ShowPanel(GameObject panel)
    {
        if (Panel_Game != null) Panel_Game.SetActive(panel == Panel_Game);
        if (Panel_GameOver != null) Panel_GameOver.SetActive(panel == Panel_GameOver);
    }

    private void InitScoreUI()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LocalScoreChanged += OnLocalScoreChanged;
            UpdateScoreUI(GameManager.Instance.Score);
        }
    }

    private void OnLocalScoreChanged(int score, float time)
    {
        UpdateScoreUI(score);
    }

    private void UpdateTargetSumUI(int targetSum)
    {
        if (txt_targetSum != null) txt_targetSum.text = targetSum.ToString();
    }

    // 막혀도 판은 끝나지 않음. 남은 스킬이 있으면 스킬 버튼을 반짝여 알려 줌.
    private void OnNoMoreMoves()
    {
        SkillManager skills = SkillManager.Instance;
        if (skills != null && skills.HasAnySkillLeft)
        {
            ShowWarningText("더 이상 만들 수 있는 수가 없습니다!\n스킬을 사용하세요");
            skills.HighlightAvailableSkills();
        }
        else
        {
            ShowWarningText("더 이상 만들 수 있는 수가 없습니다!");
        }
    }

    public void UpdateScoreUI(int newScore)
    {
        if (txt_score != null)
        {
            txt_score.text = newScore.ToString();
        }

        if (txt_gameOverscore != null)
        {
            txt_gameOverscore.text = newScore.ToString();
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (isTimerRunning && m_timerSlider != null)
        {
            float maxTime = GameManager.Instance.MaxGameTime;
            if (maxTime > 0f)
            {
                m_timerSlider.value = GameManager.Instance.CurrentTime / maxTime;
            }
        }
    }

    private void RestartGame()
    {
        ShowPanel(Panel_Game);
        if (m_score != null) m_score.SetActive(true);
        GameManager.Instance.RestartGame();
    }

    public void UpdateTimerUI(float remainingTime)
    {
        if (txt_timer != null) txt_timer.text = "TIME  " + Mathf.CeilToInt(remainingTime).ToString();

        if (GameManager.Instance != null && GameManager.Instance.MaxGameTime > 0f && m_timerSlider != null)
        {
            float maxTime = GameManager.Instance.MaxGameTime;
            m_timerSlider.value = remainingTime / maxTime;
        }

        if (remainingTime <= 0f)
        {
            isTimerRunning = false;
        }
        else
        {
            isTimerRunning = true;
        }
    }

    public void ShowWarningText(string message = "더 이상 만들 수 있는 수가 없습니다!")
    {
        if (txt_warning == null) return;

        // 이미 실행 중인 경고 연출이 있다면 중단하고 새로 시작
        warningTween?.Kill();

        txt_warning.text = message;
        txt_warning.gameObject.SetActive(true);

        // --- 연출 설정 수치 ---
        float moveDistancePhase1 = 30f; // 1단계 상승 거리 (픽셀)
        float moveDistancePhase2 = 30f; // 2단계 상승 거리 (픽셀)
        float fadeDuration = 1f;       // 페이드 인/아웃에 걸리는 시간 (초)
        float pauseDuration = 0.8f;      // 중간 일시 정지 대기 시간 (초)

        RectTransform rectTransform = txt_warning.rectTransform;
        Vector2 midPos = warningHomePosition + new Vector2(0f, moveDistancePhase1);
        Vector2 endPos = midPos + new Vector2(0f, moveDistancePhase2);

        rectTransform.anchoredPosition = warningHomePosition;
        Color transparent = txt_warning.color;
        transparent.a = 0f;
        txt_warning.color = transparent;

        warningTween = DOTween.Sequence();
        warningTween.Append(rectTransform.DOAnchorPos(midPos, fadeDuration).SetEase(Ease.Linear));
        warningTween.Join(FadeWarningText(1f, fadeDuration));
        warningTween.AppendInterval(pauseDuration);
        warningTween.Append(rectTransform.DOAnchorPos(endPos, fadeDuration).SetEase(Ease.Linear));
        warningTween.Join(FadeWarningText(0f, fadeDuration));
        warningTween.OnComplete(() =>
        {
            txt_warning.gameObject.SetActive(false);
            rectTransform.anchoredPosition = warningHomePosition; // 다음 연출을 위해 원래 위치로 복귀
            warningTween = null;
        });
    }

    // TMP 페이드는 DOTween Pro 기능이라 무료 버전의 범용 트윈으로 함.
    private Tween FadeWarningText(float alpha, float duration)
    {
        return DOTween.ToAlpha(() => txt_warning.color, color => txt_warning.color = color, alpha, duration)
            .SetEase(Ease.Linear);
    }

    private void GameOverUI()
    {
        isTimerRunning = false;
        if (Panel_GameOver == null) return;

        ShowPanel(Panel_GameOver);
        if (m_score != null) m_score.SetActive(false);
    }

    protected override void OnDestroy()
    {
        countdownTween?.Kill();
        warningTween?.Kill();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LocalScoreChanged -= OnLocalScoreChanged;
            GameManager.Instance.OnGameOver -= GameOverUI;
            GameManager.Instance.OnTimerUpdated -= UpdateTimerUI;
            GameManager.Instance.TargetSumChanged -= UpdateTargetSumUI;
            GameManager.Instance.NoMoreMoves -= OnNoMoreMoves;
        }

        base.OnDestroy();
    }
}
