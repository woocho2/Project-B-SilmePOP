using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager_Game : MonoBehaviour
{
    [Header("Audio Volume Sliders")]
    [SerializeField] private Slider slider_masterVolume;
    [SerializeField] private Slider slider_bgmVolume;
    [SerializeField] private Slider slider_sfxVolume;
    public static UIManager_Game Instance { get; private set; }

    public event UnityAction<Color> OnColorChanged;
    public event UnityAction<Sprite> OnFaceChanged;
    public event UnityAction<Sprite> OnCostumeChanged;
    public event UnityAction<ThemeData> OnMapChanged;

    [SerializeField] private DragManager m_dragManager;

    [Header("Start Countdown")]
    [SerializeField] private TextMeshProUGUI txt_startCountdown;
    [SerializeField, Min(0.1f)] private float countdownInterval = 1f;
    [SerializeField, Min(0.1f)] private float gameStartDuration = 0.7f;
    private Coroutine co_startCountdown;
    private bool isCountingDown;

    [Header("Game Panel")]
    [SerializeField] private GameObject Panel_Game;
    [SerializeField] private TextMeshProUGUI txt_timer;
    [SerializeField] private TextMeshProUGUI txt_score;
    [SerializeField] private Slider m_timerSlider;
    [SerializeField] private Button btn_menu;
    [SerializeField] private Button btn_magnifier;
    [SerializeField] private Button btn_mix;
    [SerializeField] private Button btn_destroy;
    [SerializeField] private TextMeshProUGUI txt_magnifier;
    [SerializeField] private TextMeshProUGUI txt_mix;
    [SerializeField] private TextMeshProUGUI txt_destroy;
    [SerializeField] private GameObject m_score;
    [SerializeField] private TextMeshProUGUI txt_warning;

    [Header("MainMenu Panel")]
    [SerializeField] private GameObject Panel_Main;
    [SerializeField] private Button btn_Resume;
    [SerializeField] private Button btn_Restart;
    [SerializeField] private Button btn_Option;
    [SerializeField] private Button btn_Home;

    [Header("Game Option Panel")]
    [SerializeField] private GameObject Panel_Option;
    [SerializeField] private Button btn_quit;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject Panel_GameOver;
    [SerializeField] private GameObject m_gameoverscore;
    [SerializeField] private Button btn_GameOverRestart;
    [SerializeField] private Button btn_GameOverHome;
    [SerializeField] private TextMeshProUGUI txt_gameOverscore;

    [Header("Slime Color Buttons")]
    [SerializeField] private Button btn_slimeWhite;
    [SerializeField] private Button btn_slimeBlack;
    [SerializeField] private Button btn_slimeRed;
    [SerializeField] private Button btn_slimeBlue;
    [SerializeField] private Button btn_slimeYellow;
    [SerializeField] private Button btn_slimeGreen;
    [SerializeField] private Button btn_slimeYellowGreen;
    [SerializeField] private Button btn_slimeBrown;

    [Header("Custom Color Settings")]
    [SerializeField] private Button btn_slimeCustom;
    [SerializeField] private Button btn_slimeCustomApply;
    [SerializeField] private GameObject ColorPallet;
    [SerializeField] private FlexibleColorPicker fcp;

    [Header("Slime Face Settings")]
    [SerializeField] private Button btn_slimeFaceNone;
    [SerializeField] private Button btn_slimeNormal;
    [SerializeField] private Sprite spr_slimeFaceNone;
    [SerializeField] private Sprite spr_slimeNormal;

    [Header("Slime Costume Settings")]
    [SerializeField] private Button btn_slimeCostumeNone;
    [SerializeField] private Button btn_slimeDemon;
    [SerializeField] private Button btn_slimeAngel;
    [SerializeField] private Button btn_slimeKing;
    [SerializeField] private Sprite spr_slimeCostumeNone;
    [SerializeField] private Sprite spr_slimeDemon;
    [SerializeField] private Sprite spr_slimeAngel;
    [SerializeField] private Sprite spr_slimeKing;

    [Header("Map Settings")]
    [SerializeField] private Button btn_basic;
    [SerializeField] private Button btn_spring;
    [SerializeField] private Button btn_summer;
    [SerializeField] private Button btn_autumn;
    [SerializeField] private Button btn_winter;
    [SerializeField] private Button btn_sunny;
    [SerializeField] private Button btn_night;
    [SerializeField] private Button btn_cloudy;

    [Header("Map Theme Data (SO)")]
    [SerializeField] private ThemeData themeBasic;
    [SerializeField] private ThemeData themeSpring;
    [SerializeField] private ThemeData themeSummer;
    [SerializeField] private ThemeData themeAutumn;
    [SerializeField] private ThemeData themeWinter;
    [SerializeField] private ThemeData themeSunny;
    [SerializeField] private ThemeData themeNight;
    [SerializeField] private ThemeData themeCloudy;

    private int countMagnifier;
    private int countMix;
    private int countDestroy;

    [SerializeField] string m_sceneName;

    private bool isTimerRunning = false;

    private Coroutine co_warningAnimation;
    private Coroutine co_skillHighlight;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        AudioVolumeSlider.Bind(slider_masterVolume, AudioVolumeSlider.VolumeChannel.Master);
        AudioVolumeSlider.Bind(slider_bgmVolume, AudioVolumeSlider.VolumeChannel.BGM);
        AudioVolumeSlider.Bind(slider_sfxVolume, AudioVolumeSlider.VolumeChannel.SFX);
        ResetSkillCounts();

        InitTimerUI();
        InitScoreUI();
        ApplySavedMapUI();
        InitUI();
        BindMenuButtons();
        BindSlimeColorButtons();
        BindSlimeFaceButtons();
        BindMapButtons();

        if (fcp != null)
        {
            fcp.onColorChange.AddListener(ChangeCustomSlimeColor);
            fcp.onColorChange.AddListener(ChangeCustomButtonColor);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += GameOverUI;
        }
        BeginStartCountdown();
    }

    public void BeginStartCountdown()
    {
        if (co_startCountdown != null) StopCoroutine(co_startCountdown);
        co_startCountdown = null;
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
        SetCountdownButtons(false);
        co_startCountdown = StartCoroutine(Co_StartCountdown());
    }

    private IEnumerator Co_StartCountdown()
    {
        txt_startCountdown.raycastTarget = false;
        txt_startCountdown.gameObject.SetActive(true);
        var interval = new WaitForSecondsRealtime(Mathf.Max(0.1f, countdownInterval));
        for (int count = 3; count >= 1; count--)
        {
            txt_startCountdown.text = count + "!";
            SoundManager.Play(SoundEffect.Countdown);
            yield return interval;
        }

        txt_startCountdown.text = "GameStart!";
        SoundManager.Play(SoundEffect.GameStart);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, gameStartDuration));
        txt_startCountdown.gameObject.SetActive(false);
        isCountingDown = false;
        co_startCountdown = null;
        SetCountdownButtons(true);
        if (GameManager.Instance != null) GameManager.Instance.StartGameTimer();
    }

    private void SetCountdownButtons(bool ready)
    {
        if (btn_menu != null) btn_menu.interactable = ready;
        if (btn_magnifier != null) btn_magnifier.interactable = ready && countMagnifier > 0;
        if (btn_mix != null) btn_mix.interactable = ready && countMix > 0;
        if (btn_destroy != null) btn_destroy.interactable = ready && countDestroy > 0;
    }

    private void OnDisable()
    {
        if (co_startCountdown != null) StopCoroutine(co_startCountdown);
        co_startCountdown = null;
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
        if (Panel_Main != null) Panel_Main.SetActive(false);
        if (Panel_Option != null) Panel_Option.SetActive(false);
        if (Panel_Game != null) Panel_Game.SetActive(true);
        if (Panel_GameOver != null) Panel_GameOver.SetActive(false);

        if (ColorPallet != null) ColorPallet.SetActive(false);
        if (txt_warning != null) txt_warning.gameObject.SetActive(false);
    }

    private void InitScoreUI()
    {
        if (m_dragManager != null)
        {
            m_dragManager.OnScoreChanged += UpdateScoreUI;
            UpdateScoreUI(m_dragManager.totalScore);
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

    private void UpdateSkillCountUI()
    {
        if (txt_magnifier != null) txt_magnifier.text = $"{countMagnifier.ToString()}/1";
        if (txt_mix != null) txt_mix.text = $"{countMix.ToString()}/1";
        if (txt_destroy != null) txt_destroy.text = $"{countDestroy.ToString()}/3";
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsPaused) return;
        if (isTimerRunning && m_timerSlider != null && GameManager.Instance != null)
        {
            float maxTime = GameManager.Instance.MaxGameTime;
            if (maxTime > 0f)
            {
                m_timerSlider.value = 1f - GameManager.Instance.CurrentTime / maxTime;
            }
        }

        if (isTimerRunning && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                UseMagnifier();
            }
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                UseMix();
            }
            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                UseDestroy();
            }
        }
    }

    public bool HasAnySkillLeft()
    {
        return countMagnifier > 0 || countMix > 0 || countDestroy > 0;
    }

    private void UseMagnifier()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsPaused) return;
        if (countMagnifier > 0 && SlimeManager.Instance != null)
        {
            SoundManager.Play(SoundEffect.Hint);
            SlimeManager.Instance.ShowHint();
            countMagnifier--;

            UpdateSkillCountUI();
                        
            if (countMagnifier <= 0 && btn_magnifier != null) btn_magnifier.interactable = false;
        }
    }

    private void UseMix()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsPaused) return;
        if (countMix > 0 && SlimeManager.Instance != null)
        {
            SoundManager.Play(SoundEffect.Mix);
            SlimeManager.Instance.MixSlimes();
            countMix--;

            UpdateSkillCountUI();

            if (countMix <= 0 && btn_mix != null) btn_mix.interactable = false;
        }
    }

    private void UseDestroy()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsPaused) return;
        if (countDestroy > 0 && SlimeManager.Instance != null)
        {
            SlimeManager.Instance.ActivateDestroySkill();
            countDestroy--;

            UpdateSkillCountUI();

            if (countDestroy <= 0 && btn_destroy != null) btn_destroy.interactable = false;
        }
    }

    private void ResetSkillCounts()
    {
        countMagnifier = 1;
        countMix = 1;
        countDestroy = 3;

        if (btn_magnifier != null) btn_magnifier.interactable = true;
        if (btn_mix != null) btn_mix.interactable = true;
        if (btn_destroy != null) btn_destroy.interactable = true;

        UpdateSkillCountUI();
    }

    private void BindMenuButtons()
    {
        if (btn_menu != null)
        {
            btn_menu.onClick.RemoveAllListeners();
            btn_menu.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_menu.onClick.AddListener(() =>
            {
                Panel_Game.SetActive(false);
                Panel_Option.SetActive(false);
                Panel_Main.SetActive(true);
                Panel_GameOver.SetActive(false);
                GameManager.Instance.PauseGame();
            });
        }

        if (btn_magnifier != null)
        {
            btn_magnifier.onClick.RemoveAllListeners();
            btn_magnifier.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_magnifier.onClick.AddListener(UseMagnifier);
        }

        if (btn_mix != null)
        {
            btn_mix.onClick.RemoveAllListeners();
            btn_mix.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_mix.onClick.AddListener(UseMix);
        }

        if (btn_destroy != null)
        {
            btn_destroy.onClick.RemoveAllListeners();
            btn_destroy.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_destroy.onClick.AddListener(UseDestroy);
        }

        BindResumeButtons();
        BindRestartButtons();
        BindOptionButtons();
        BindHomeButtons();
        BindGameOverRestartButtons();
        BindGameOverHomeButtons();
    }

    private void BindResumeButtons()
    {
        if (btn_Resume != null)
        {
            btn_Resume.onClick.RemoveAllListeners();
            btn_Resume.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_Resume.onClick.AddListener(() =>
            {
                Panel_Game.SetActive(true);
                Panel_Option.SetActive(false);
                Panel_Main.SetActive(false);
                Panel_GameOver.SetActive(false);
                GameManager.Instance.ResumeGame();
            });
        }
    }

    private void BindRestartButtons()
    {
        if (btn_Restart != null)
        {
            btn_Restart.onClick.RemoveAllListeners();
            btn_Restart.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_Restart.onClick.AddListener(() =>
            {
                Panel_Game.SetActive(true);
                Panel_Option.SetActive(false);
                Panel_Main.SetActive(false);
                Panel_GameOver.SetActive(false);
                m_score.SetActive(true);
                ResetSkillCounts();
                GameManager.Instance.RestartGame();
            });
        }
    }

    private void BindGameOverRestartButtons()
    {
        if (btn_GameOverRestart != null)
        {
            btn_GameOverRestart.onClick.RemoveAllListeners();
            btn_GameOverRestart.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_GameOverRestart.onClick.AddListener(() =>
            {
                Panel_Game.SetActive(true);
                Panel_Option.SetActive(false);
                Panel_Main.SetActive(false);
                Panel_GameOver.SetActive(false);
                m_score.SetActive(true);
                ResetSkillCounts();
                GameManager.Instance.RestartGame();
            });
        }
    }

    private void BindOptionButtons()
    {
        if (btn_Option != null)
        {
            btn_Option.onClick.RemoveAllListeners();
            btn_Option.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_Option.onClick.AddListener(() =>
            {
                Panel_Game.SetActive(false);
                Panel_Option.SetActive(true);
                Panel_Main.SetActive(false);
            });
        }

        if (btn_quit != null)
        {
            btn_quit.onClick.RemoveAllListeners();
            btn_quit.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_quit.onClick.AddListener(() =>
            {
                Panel_Game.SetActive(false);
                Panel_Option.SetActive(false);
                Panel_Main.SetActive(true);

                OnColorChanged?.Invoke(ChangeData.SelectedColor);
                if (ChangeData.SelectedFace != null) OnFaceChanged?.Invoke(ChangeData.SelectedFace);
                if (ChangeData.SelectedCostume != null) OnCostumeChanged?.Invoke(ChangeData.SelectedCostume);
                if (ChangeData.SelectedMapTheme != null) OnMapChanged?.Invoke(ChangeData.SelectedMapTheme);
            });
        }
    }

    private void BindHomeButtons()
    {
        if (btn_Home == null) return;
        btn_Home.onClick.RemoveAllListeners();
        btn_Home.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
        btn_Home.onClick.AddListener(() =>
        {
            SceneLoader.StartLoad(m_sceneName);
        });
    }

    private void BindGameOverHomeButtons()
    {
        if (btn_GameOverHome == null) return;
        btn_GameOverHome.onClick.RemoveAllListeners();
        btn_GameOverHome.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
        btn_GameOverHome.onClick.AddListener(() =>
        {
            SceneLoader.StartLoad(m_sceneName);
        });
    }

    private void BindSlimeColorButtons()
    {
        BindColor(btn_slimeWhite, ChangeData.HEX_WHITE);
        BindColor(btn_slimeBlack, ChangeData.HEX_BLACK);
        BindColor(btn_slimeRed, ChangeData.HEX_RED);
        BindColor(btn_slimeBlue, ChangeData.HEX_BLUE);
        BindColor(btn_slimeYellow, ChangeData.HEX_YELLOW);
        BindColor(btn_slimeGreen, ChangeData.HEX_GREEN);
        BindColor(btn_slimeYellowGreen, ChangeData.HEX_YELLOWGREEN);
        BindColor(btn_slimeBrown, ChangeData.HEX_BROWN);

        if (btn_slimeCustom != null && ColorPallet != null && btn_slimeCustomApply != null)
        {
            btn_slimeCustom.onClick.RemoveAllListeners();
            btn_slimeCustom.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_slimeCustom.onClick.AddListener(() =>
            {
                bool willBeActive = !ColorPallet.activeSelf;
                Color backupColor = ChangeData.LastCustomColor;
                ColorPallet.SetActive(willBeActive);

                if (btn_slimeCustomApply != null)
                {
                    btn_slimeCustomApply.gameObject.SetActive(willBeActive);
                }

                if (willBeActive && fcp != null)
                {
                    ChangeData.LastCustomColor = backupColor;
                    fcp.color = backupColor;
                }
            });
        }

        if (btn_slimeCustomApply != null) {
            btn_slimeCustomApply.onClick.RemoveAllListeners();
            btn_slimeCustomApply.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_slimeCustomApply.onClick.AddListener(OnCustomApplyClicked);
        }
    }
    private void OnCustomApplyClicked()
    {
        if (ColorPallet != null)
        {
            ColorPallet.SetActive(false);
        }

        if (btn_slimeCustomApply != null)
        {
            btn_slimeCustomApply.gameObject.SetActive(false);
        }
    }

    private void BindSlimeFaceButtons()
    {
        BindSprite(btn_slimeFaceNone, spr_slimeFaceNone, ChangeSlimeFace);
        BindSprite(btn_slimeNormal, spr_slimeNormal, ChangeSlimeFace);

        BindSprite(btn_slimeCostumeNone, spr_slimeCostumeNone, ChangeSlimeCostume);
        BindSprite(btn_slimeDemon, spr_slimeDemon, ChangeSlimeCostume);
        BindSprite(btn_slimeAngel, spr_slimeAngel, ChangeSlimeCostume);
        BindSprite(btn_slimeKing, spr_slimeKing, ChangeSlimeCostume);
    }

    private void BindMapButtons()
    {
        BindMap(btn_basic, themeBasic);
        BindMap(btn_spring, themeSpring);
        BindMap(btn_summer, themeSummer);
        BindMap(btn_autumn, themeAutumn);
        BindMap(btn_winter, themeWinter);
        BindMap(btn_sunny, themeSunny);
        BindMap(btn_night, themeNight);
        BindMap(btn_cloudy, themeCloudy);
    }

    private void BindColor(Button btn, string hexCode)
    {
        if (btn != null && ColorUtility.TryParseHtmlString(hexCode, out Color parsedColor))
        {
            parsedColor.a = ChangeData.SLIME_ALPHA;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn.onClick.AddListener(() => ChangeSlimeColor(parsedColor));
        }
    }

    private void BindSprite(Button btn, Sprite sprite, UnityAction<Sprite> action)
    {
        if (btn == null || sprite == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
        btn.onClick.AddListener(() => action(sprite));
    }

    private void BindMap(Button btn, ThemeData theme)
    {
        if (btn == null || theme == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
        btn.onClick.AddListener(() => ChangeMap(theme));
    }

    private void ChangeSlimeColor(Color newColor)
    {
        newColor.a = ChangeData.SLIME_ALPHA;
        ChangeData.SelectedColor = newColor;
        OnColorChanged?.Invoke(newColor);
    }

    private void ChangeCustomSlimeColor(Color newColor)
    {
        newColor.a = ChangeData.SLIME_ALPHA;
        ChangeData.SelectedColor = newColor;
        ChangeData.LastCustomColor = newColor;
        OnColorChanged?.Invoke(newColor);
    }

    private void ChangeCustomButtonColor(Color newColor)
    {
        if (btn_slimeCustom != null && btn_slimeCustom.image != null)
        {
            newColor.a = 1.0f;
            btn_slimeCustom.image.color = newColor;
        }
    }

    private void ChangeSlimeFace(Sprite newSprite)
    {
        if (newSprite == null) return;
        ChangeData.SelectedFace = newSprite;
        OnFaceChanged?.Invoke(newSprite);
    }

    private void ChangeSlimeCostume(Sprite newSprite)
    {
        if (newSprite == null) return;
        ChangeData.SelectedCostume = newSprite;
        OnCostumeChanged?.Invoke(newSprite);
    }

    private void ChangeMap(ThemeData newTheme)
    {
        if (newTheme == null) return;

        ChangeData.SelectedMapTheme = newTheme;

        if (Panel_Option != null)
        {
            Panel_Option.GetComponent<Image>().sprite = newTheme.TableSprite;
        }

        OnMapChanged?.Invoke(newTheme);
    }

    public void UpdateTimerUI(float remainingTime)
    {
        if (txt_timer != null) txt_timer.text = Mathf.CeilToInt(remainingTime).ToString();

        if (GameManager.Instance != null && GameManager.Instance.MaxGameTime > 0f && m_timerSlider != null)
        {
            float maxTime = GameManager.Instance.MaxGameTime;
            m_timerSlider.value = (maxTime - remainingTime) / maxTime;
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

    private void ApplySavedMapUI()
    {
        if (Panel_Option != null && ChangeData.SelectedMapTheme != null)
        {
            Panel_Option.GetComponent<Image>().sprite = ChangeData.SelectedMapTheme.TableSprite;
        }
    }

    public void ShowWarningText(string message = "더 이상 맞출 수 있는 슬라임이 없습니다!")
    {
        if (txt_warning == null) return;

        // 이미 실행 중인 경고 애니메이션이 있다면 중단하고 새로 시작
        if (co_warningAnimation != null)
        {
            StopCoroutine(co_warningAnimation);
        }

        co_warningAnimation = StartCoroutine(Co_AnimateWarningText(message));
    }

    private IEnumerator Co_AnimateWarningText(string message)
    {
        txt_warning.text = message;
        txt_warning.gameObject.SetActive(true);

        // --- 연출 설정 수치 ---
        float moveDistancePhase1 = 30f; // 1단계 상승 거리 (픽셀)
        float moveDistancePhase2 = 30f; // 2단계 상승 거리 (픽셀)
        float fadeDuration = 1f;       // 페이드 인/아웃에 걸리는 시간 (초)
        float pauseDuration = 0.8f;      // 중간 일시 정지 대기 시간 (초)

        // 초기 위치 저장 및 시작 위치 설정
        RectTransform rectTransform = txt_warning.rectTransform;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 midPos = startPos + new Vector2(0f, moveDistancePhase1);
        Vector2 endPos = midPos + new Vector2(0f, moveDistancePhase2);

        // 1단계: 투명 -> 불투명 (Fade In) + 위로 이동
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);

            // Lerp를 이용해 위치 및 투명도 부드럽게 변경
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, midPos, progress);
            txt_warning.color = new Color(txt_warning.color.r, txt_warning.color.g, txt_warning.color.b, progress);

            yield return null;
        }

        // 1단계 완료 보정
        rectTransform.anchoredPosition = midPos;
        txt_warning.color = new Color(txt_warning.color.r, txt_warning.color.g, txt_warning.color.b, 1f);

        // 중간 일시 정지
        yield return new WaitForSeconds(pauseDuration);

        // 2단계: 불투명 -> 투명 (Fade Out) + 추가 위로 이동
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);

            rectTransform.anchoredPosition = Vector2.Lerp(midPos, endPos, progress);
            txt_warning.color = new Color(txt_warning.color.r, txt_warning.color.g, txt_warning.color.b, 1f - progress);

            yield return null;
        }

        // 연출 종료 후 정리
        txt_warning.gameObject.SetActive(false);
        rectTransform.anchoredPosition = startPos; // 다음 연출을 위해 원래 위치로 복귀
        co_warningAnimation = null;
    }

    public void HighlightAvailableSkills()
    {
        if (co_skillHighlight != null)
        {
            StopCoroutine(co_skillHighlight);
        }

        co_skillHighlight = StartCoroutine(Co_HighlightSkills());
    }

    private IEnumerator Co_HighlightSkills()
    {
        // 강조 효과를 줄 잔여 스킬 버튼 리스트 추출
        List<Button> targetButtons = new List<Button>();
        if (countMagnifier > 0 && btn_magnifier != null) targetButtons.Add(btn_magnifier);
        if (countMix > 0 && btn_mix != null) targetButtons.Add(btn_mix);
        if (countDestroy > 0 && btn_destroy != null) targetButtons.Add(btn_destroy);

        if (targetButtons.Count == 0) yield break;

        // 버튼 원래 색상 저장 (기본 흰색 이미지 기준)
        Color originColor = Color.white;
        Color highlightColor = new Color(1f, 0.9f, 0.3f, 1f); // 노란색 강조 빛

        float duration = 0.4f; // 반짝이는 속도
        int repeatCount = 3;   // 반짝이는 횟수

        for (int i = 0; i < repeatCount; i++)
        {
            // 노랗게 강조
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                Color currentColor = Color.Lerp(originColor, highlightColor, timer / duration);

                foreach (Button btn in targetButtons)
                {
                    if (btn.image != null) btn.image.color = currentColor;
                }
                yield return null;
            }

            // 원래 색으로 복귀
            timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                Color currentColor = Color.Lerp(highlightColor, originColor, timer / duration);

                foreach (Button btn in targetButtons)
                {
                    if (btn.image != null) btn.image.color = currentColor;
                }
                yield return null;
            }
        }

        // 최종 색상 원복 보정
        foreach (Button btn in targetButtons)
        {
            if (btn.image != null) btn.image.color = originColor;
        }

        co_skillHighlight = null;
    }

    private void GameOverUI()
    {
        isTimerRunning = false;
        if (Panel_Main != null && Panel_Option != null && Panel_GameOver)
        {
            Panel_Main.SetActive(false);
            Panel_Option.SetActive(false);
            Panel_Game.SetActive(false);
            Panel_GameOver.SetActive(true);
            m_score.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (m_dragManager != null) m_dragManager.OnScoreChanged -= UpdateScoreUI;
        if (GameManager.Instance != null) GameManager.Instance.OnGameOver -= GameOverUI;
        if (GameManager.Instance != null) GameManager.Instance.OnTimerUpdated -= UpdateTimerUI;

        if (fcp != null)
        {
            fcp.onColorChange.RemoveListener(ChangeCustomSlimeColor);
            fcp.onColorChange.RemoveListener(ChangeCustomButtonColor);
        }

        if (Instance == this) Instance = null;
    }
}
