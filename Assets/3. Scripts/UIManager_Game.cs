using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class UIManager_Game : MonoBehaviour
{
    public static UIManager_Game Instance { get; private set; }

    public event UnityAction<Color> OnColorChanged;
    public event UnityAction<Sprite> OnFaceChanged;
    public event UnityAction<Sprite> OnCostumeChanged;
    public event UnityAction<ThemeData> OnMapChanged;

    [SerializeField] private DragManager m_dragManager;

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
    [SerializeField] private GameObject ColorPallet;
    [SerializeField] private FlexibleColorPicker fcp;

    [Header("Slime Face Settings")]
    [SerializeField] private Button btn_slimeNone;
    [SerializeField] private Button btn_slimeNormal;

    [Header("Slime Costume Settings")]
    [SerializeField] private Button btn_slimeDemon;
    [SerializeField] private Button btn_slimeAngel;
    [SerializeField] private Button btn_slimeKing;

    [SerializeField] private Sprite spr_slimeNone;
    [SerializeField] private Sprite spr_slimeNormal;

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
        if (isTimerRunning && m_timerSlider != null && GameManager.Instance != null)
        {
            float maxTime = GameManager.Instance.MaxGameTime;
            if (maxTime > 0f)
            {
                m_timerSlider.value += (1f / maxTime) * Time.deltaTime;
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
        if (countMagnifier > 0 && SlimeManager.Instance != null)
        {
            SlimeManager.Instance.ShowHint();
            countMagnifier--;

            UpdateSkillCountUI();
                        
            if (countMagnifier <= 0 && btn_magnifier != null) btn_magnifier.interactable = false;
        }
    }

    private void UseMix()
    {
        if (countMix > 0 && SlimeManager.Instance != null)
        {
            SlimeManager.Instance.MixSlimes();
            countMix--;

            UpdateSkillCountUI();

            if (countMix <= 0 && btn_mix != null) btn_mix.interactable = false;
        }
    }

    private void UseDestroy()
    {
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
            btn_magnifier.onClick.AddListener(UseMagnifier);
        }

        if (btn_mix != null)
        {
            btn_mix.onClick.RemoveAllListeners();
            btn_mix.onClick.AddListener(UseMix);
        }

        if (btn_destroy != null)
        {
            btn_destroy.onClick.RemoveAllListeners();
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
        btn_Home.onClick.RemoveAllListeners();
        btn_Home.onClick.AddListener(() =>
        {
            SceneLoader.StartLoad(m_sceneName);
        });
    }

    private void BindGameOverHomeButtons()
    {
        btn_GameOverHome.onClick.RemoveAllListeners();
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

        if (btn_slimeCustom != null && ColorPallet != null)
        {
            btn_slimeCustom.onClick.RemoveAllListeners();
            btn_slimeCustom.onClick.AddListener(() =>
            {
                bool willBeActive = !ColorPallet.activeSelf;
                Color backupColor = ChangeData.LastCustomColor;
                ColorPallet.SetActive(willBeActive);

                if (willBeActive && fcp != null)
                {
                    ChangeData.LastCustomColor = backupColor;
                    fcp.color = backupColor;
                }
            });
        }
    }

    private void BindSlimeFaceButtons()
    {
        BindSprite(btn_slimeNone, spr_slimeNone, ChangeSlimeFace);
        BindSprite(btn_slimeNormal, spr_slimeNormal, ChangeSlimeFace);

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
            btn.onClick.AddListener(() => ChangeSlimeColor(parsedColor));
        }
    }

    private void BindSprite(Button btn, Sprite sprite, UnityAction<Sprite> action)
    {
        if (btn == null || sprite == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => action(sprite));
    }

    private void BindMap(Button btn, ThemeData theme)
    {
        if (btn == null || theme == null) return;
        btn.onClick.RemoveAllListeners();
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

    private void UpdateTimerUI(float remainingTime)
    {
        if (txt_timer != null) txt_timer.text = ((int)remainingTime).ToString();

        if (GameManager.Instance != null && GameManager.Instance.MaxGameTime > 0f && m_timerSlider != null)
        {
            float maxTime = GameManager.Instance.MaxGameTime;
            m_timerSlider.value = (maxTime - remainingTime) / maxTime;
        }

        if (remainingTime <= 0f) isTimerRunning = false;
    }

    private void ApplySavedMapUI()
    {
        if (Panel_Option != null && ChangeData.SelectedMapTheme != null)
        {
            Panel_Option.GetComponent<Image>().sprite = ChangeData.SelectedMapTheme.TableSprite;
        }
    }

    private void GameOverUI()
    {
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
        if (GameManager.Instance != null) GameManager.Instance.OnTimerUpdated -= UpdateTimerUI;

        if (fcp != null)
        {
            fcp.onColorChange.RemoveListener(ChangeCustomSlimeColor);
            fcp.onColorChange.RemoveListener(ChangeCustomButtonColor);
        }

        if (Instance == this) Instance = null;
    }
}