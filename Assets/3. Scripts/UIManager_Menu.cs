using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIManager_Menu : MonoBehaviour
{
    public static UIManager_Menu Instance { get; private set; }

    public event UnityAction<Color> OnColorChanged;
    public event UnityAction<Sprite> OnFaceChanged;
    public event UnityAction<ThemeData> OnMapChanged;

    [Header("Panels & UI Elements")]
    [SerializeField] private GameObject Panel_MainMenu;
    [SerializeField] private GameObject Panel_Option;
    [SerializeField] private GameObject Panel_Help;
    [SerializeField] private Image img_Check;
    [SerializeField] private string m_sceneName;

    [Header("Menu Buttons")]
    [SerializeField] private Button btn_GameStart;
    [SerializeField] private Button btn_GameOption;
    [SerializeField] private Button btn_GameHelp;
    [SerializeField] private Button btn_GameQuit;
    [SerializeField] private Button btn_QuitOption;

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
    [SerializeField] private FlexibleColorPicker fcp; // FCP 연동용 변수 추가

    [Header("Slime Face Settings")]
    [SerializeField] private Button btn_slimeNone;
    [SerializeField] private Button btn_slimeNormal;
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
        ApplySavedMapUI();
        InitUI();
        BindMenuButtons();
        BindSlimeColorButtons();
        BindSlimeFaceButtons();
        BindMapButtons();

        // 컬러 피커에서 색상이 변경될 때 FCP 전용 메서드 실행 (커스텀 값 저장 기능 포함)
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
        if (img_Check != null) img_Check.enabled = false;

        // 초기화 시 팔레트 창은 닫아둡니다.
        if (ColorPallet != null) ColorPallet.SetActive(false);
    }

    private void ApplySavedMapUI()
    {
        if (ChangeData.SelectedMapTheme == null) return;

        if (Panel_MainMenu != null)
        {
            Panel_MainMenu.GetComponent<Image>().sprite = ChangeData.SelectedMapTheme.mainMenuSprite;
        }

        if (Panel_Option != null)
        {
            Panel_Option.GetComponent<Image>().sprite = ChangeData.SelectedMapTheme.optionAndTableSprite;
        }
    }

    private void BindMenuButtons()
    {
        BindMenuButton(btn_GameStart, 170f, () => SceneLoader.StartLoad(m_sceneName));
        BindMenuButton(btn_GameOption, 20f, () => SwitchPanel(Panel_MainMenu, Panel_Option));
        BindMenuButton(btn_GameHelp, -130f, () => SwitchPanel(Panel_MainMenu, Panel_Help));
        BindMenuButton(btn_GameQuit, -280f);

        if (btn_QuitOption != null)
        {
            btn_QuitOption.onClick.RemoveAllListeners();
            btn_QuitOption.onClick.AddListener(() => SwitchPanel(Panel_Option, Panel_MainMenu));
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

        // 커스텀 컬러 버튼 클릭 시 팔레트를 열면서 이전에 저장된 값을 FCP에 반영
        if (btn_slimeCustom != null && ColorPallet != null)
        {
            btn_slimeCustom.onClick.RemoveAllListeners();
            btn_slimeCustom.onClick.AddListener(() =>
            {
                bool willBeActive = !ColorPallet.activeSelf;

                // 1. 창이 열리기 직전, 덮어씌워지기 전의 안전한 커스텀 컬러를 임시 백업합니다.
                Color backupColor = ChangeData.LastCustomColor;

                // 2. 창 활성화 (이 순간 FCP 내부 로직에 의해 빨간색 이벤트가 발생하며 LastCustomColor가 오염됨)
                ColorPallet.SetActive(willBeActive);

                // 3. 창이 열렸다면, 오염된 데이터를 백업본으로 복구하고 FCP UI에도 강제 적용합니다.
                if (willBeActive && fcp != null)
                {
                    ChangeData.LastCustomColor = backupColor; // 오염된 데이터 원상 복구
                    fcp.color = backupColor; // FCP에 다시 전달 (이때 올바른 색상으로 이벤트가 재발생함)
                }
            });
        }
    }

    private void BindSlimeFaceButtons()
    {
        BindSprite(btn_slimeNone, spr_slimeNone, ChangeSlimeFace);
        BindSprite(btn_slimeNormal, spr_slimeNormal, ChangeSlimeFace);
        BindSprite(btn_slimeDemon, spr_slimeDemon, ChangeSlimeFace);
        BindSprite(btn_slimeAngel, spr_slimeAngel, ChangeSlimeFace);
        BindSprite(btn_slimeKing, spr_slimeKing, ChangeSlimeFace);
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
        btn.onClick.AddListener(() => {
            MenuCheck(ypos);
            action?.Invoke();
        });
    }

    private void MenuCheck(float yPos)
    {
        if (img_Check != null)
        {
            img_Check.enabled = true;
            Vector2 position = img_Check.rectTransform.anchoredPosition;
            position.y = yPos;
            img_Check.rectTransform.anchoredPosition = position;
        }
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

    // 기본 프리셋 버튼 전용 색상 적용 메서드
    private void ChangeSlimeColor(Color newColor)
    {
        newColor.a = ChangeData.SLIME_ALPHA;
        ChangeData.SelectedColor = newColor;
        OnColorChanged?.Invoke(newColor);
    }

    // FCP(커스텀 컬러 팔레트) 전용 색상 적용 메서드 (LastCustomColor 저장 기능 포함)[cite: 37]
    private void ChangeCustomSlimeColor(Color newColor)
    {
        newColor.a = ChangeData.SLIME_ALPHA;
        ChangeData.SelectedColor = newColor;
        ChangeData.LastCustomColor = newColor; // 컬러 피커에서 선택한 값을 따로 저장[cite: 37]
        OnColorChanged?.Invoke(newColor);
    }

    private void ChangeCustomButtonColor(Color newColor)
    {
        if (btn_slimeCustom != null && btn_slimeCustom.image != null)
        {
            // 버튼이 투명해지지 않도록 알파값을 완전히 불투명(1.0f)하게 고정
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

    private void ChangeMap(ThemeData newTheme)
    {
        if (newTheme == null) return;

        ChangeData.SelectedMapTheme = newTheme;

        if (Panel_MainMenu != null) Panel_MainMenu.GetComponent<Image>().sprite = newTheme.mainMenuSprite;
        if (Panel_Option != null) Panel_Option.GetComponent<Image>().sprite = newTheme.optionAndTableSprite;

        OnMapChanged?.Invoke(newTheme);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지용 이벤트 리스너 해제
        if (fcp != null)
        {
            fcp.onColorChange.RemoveListener(ChangeCustomSlimeColor); // 변경된 메서드로 해제
            fcp.onColorChange.RemoveListener(ChangeCustomButtonColor);
        }

        if (Instance == this) Instance = null;
    }
}