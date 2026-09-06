using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIManager_Menu : MonoBehaviour
{
    [Header("Audio Volume Sliders")]
    [SerializeField] private Slider slider_masterVolume;
    [SerializeField] private Slider slider_bgmVolume;
    [SerializeField] private Slider slider_sfxVolume;
    public static UIManager_Menu Instance { get; private set; }

    public event UnityAction<Color> OnColorChanged;
    public event UnityAction<Sprite> OnFaceChanged;
    public event UnityAction<Sprite> OnCostumeChanged; // 코스튬 이벤트 추가
    public event UnityAction<ThemeData> OnMapChanged;

    [Header("Panels & UI Elements")]
    [SerializeField] private GameObject Panel_MainMenu;
    [SerializeField] private GameObject Panel_Option;
    [SerializeField] private GameObject Panel_Help;
    [SerializeField] private string m_sceneName;

    [Header("Menu Buttons")]
    [SerializeField] private Button btn_gameStart;
    [SerializeField] private Button btn_option;
    [SerializeField] private Button btn_help;
    [SerializeField] private Button btn_quit;
    [SerializeField] private Button btn_optionQuit;

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
    [SerializeField] private Button btn_slimeNone;
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
    }

    private void InitUI()
    {
        if (Panel_MainMenu != null) Panel_MainMenu.SetActive(true);
        if (Panel_Option != null) Panel_Option.SetActive(false);
        if (Panel_Help != null) Panel_Help.SetActive(false);

        if (ColorPallet != null) ColorPallet.SetActive(false);
    }

    private void ApplySavedMapUI()
    {
        if (ChangeData.SelectedMapTheme == null) return;

        if (Panel_MainMenu != null)
        {
            Panel_MainMenu.GetComponent<Image>().sprite = ChangeData.SelectedMapTheme.MenuSprite;
        }

        if (Panel_Option != null)
        {
            Panel_Option.GetComponent<Image>().sprite = ChangeData.SelectedMapTheme.TableSprite;
        }
    }

    private void BindMenuButtons()
    {
        BindMenuButton(btn_gameStart, 170f, () => SceneLoader.StartLoad(m_sceneName));
        BindMenuButton(btn_option, 20f, () => SwitchPanel(Panel_MainMenu, Panel_Option));
        BindMenuButton(btn_help, -130f, () => SwitchPanel(Panel_MainMenu, Panel_Help));
        BindMenuButton(btn_quit, -280f);

        if (btn_optionQuit != null)
        {
            btn_optionQuit.onClick.RemoveAllListeners();
            btn_optionQuit.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_optionQuit.onClick.AddListener(() => SwitchPanel(Panel_Option, Panel_MainMenu));
        }
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

        if (btn_slimeCustomApply != null)
        {
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
        // 얼굴은 Face로
        BindSprite(btn_slimeNone, spr_slimeFaceNone, ChangeSlimeFace);
        BindSprite(btn_slimeNormal, spr_slimeNormal, ChangeSlimeFace);

        // 악마, 천사, 왕관은 Costume으로 분리 연결
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

    private void SwitchPanel(GameObject from, GameObject to)
    {
        if (from != null) from.SetActive(false);
        if (to != null) to.SetActive(true);
    }

    private void BindMenuButton(Button btn, float ypos, UnityAction action = null)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
        btn.onClick.AddListener(() => {
            action?.Invoke();
        });
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

    // 코스튬 전용 메서드 추가
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

        if (Panel_MainMenu != null) Panel_MainMenu.GetComponent<Image>().sprite = newTheme.MenuSprite;
        if (Panel_Option != null) Panel_Option.GetComponent<Image>().sprite = newTheme.TableSprite;

        OnMapChanged?.Invoke(newTheme);
    }

    private void OnDestroy()
    {
        if (fcp != null)
        {
            fcp.onColorChange.RemoveListener(ChangeCustomSlimeColor);
            fcp.onColorChange.RemoveListener(ChangeCustomButtonColor);
        }

        if (Instance == this) Instance = null;
    }
}
