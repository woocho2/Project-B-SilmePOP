using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 옵션 패널: BGM·SFX 볼륨, 슬라임 색(컬러피커), 맵 테마, 닫기. OptionPanel(프리팹)에 붙임.
// 닫기 버튼이나 옵션 창 바깥의 빈 곳을 누르면 닫힘 (컬러피커가 떠 있을 때는 컬러피커가 먼저).
// 볼륨 슬라이더의 Fill 색은 ThemeSkin, 슬라임 색은 MySlimeColor가 맡음.
public class OptionPanel : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Slider slider_bgmVolume;
    [SerializeField] private Slider slider_sfxVolume;
    [Tooltip("볼륨 숫자 (0 ~ 100)")]
    [SerializeField] private TMP_Text txt_bgmValue;
    [SerializeField] private TMP_Text txt_sfxValue;

    [Header("Color")]
    [Tooltip("Color / Slime 버튼. 누르면 컬러피커를 엶")]
    [SerializeField] private Button btn_slimeColor;
    [SerializeField] private PastelColorPicker slimeColorPicker;

    [Header("Map (ThemePalette 순서)")]
    [SerializeField] private Button btn_basic;
    [SerializeField] private Button btn_spring;
    [SerializeField] private Button btn_summer;
    [SerializeField] private Button btn_autumn;
    [SerializeField] private Button btn_winter;
    [SerializeField] private Button btn_sunny;
    [SerializeField] private Button btn_night;
    [SerializeField] private Button btn_cloudy;

    [Header("Quit")]
    [SerializeField] private Button btn_optionQuit;

    private int openedFrame = -1;   // 연 그 클릭이 곧바로 "바깥 클릭"으로 잡혀 닫히지 않게

    private void OnEnable()
    {
        openedFrame = Time.frameCount;
    }

    private void Update()
    {
        if (PanelTransition.IsShown(gameObject) && PressedOutside()) Close();
    }

    // 옵션 창(이 오브젝트의 사각형) 바깥을 눌렀는가
    private bool PressedOutside()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame || Time.frameCount == openedFrame) return false;
        if (slimeColorPicker != null && slimeColorPicker.IsOpen) return false;   // 컬러피커 배경을 누른 것

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, pointer.position.ReadValue(), eventCamera);
    }

    private void Awake()
    {
        AudioVolumeSlider.Bind(slider_bgmVolume, AudioVolumeSlider.VolumeChannel.BGM, txt_bgmValue);
        AudioVolumeSlider.Bind(slider_sfxVolume, AudioVolumeSlider.VolumeChannel.SFX, txt_sfxValue);

        // 비워 두면 옵션 패널 안에 넣어 둔 컬러피커를 찾음
        if (slimeColorPicker == null) slimeColorPicker = GetComponentInChildren<PastelColorPicker>(true);
        UIButtons.Bind(btn_slimeColor, OpenColorPicker);

        BindMap(btn_basic, 0);
        BindMap(btn_spring, 1);
        BindMap(btn_summer, 2);
        BindMap(btn_autumn, 3);
        BindMap(btn_winter, 4);
        BindMap(btn_sunny, 5);
        BindMap(btn_night, 6);
        BindMap(btn_cloudy, 7);

        UIButtons.Bind(btn_optionQuit, Close);
    }

    private void OpenColorPicker()
    {
        if (slimeColorPicker == null)
        {
            Debug.LogWarning("OptionPanel: 컬러피커(PastelColorPicker)를 찾지 못했습니다. slimeColorPicker를 연결해 주세요.", this);
            return;
        }
        slimeColorPicker.Open();
    }

    private static void BindMap(Button button, int themeIndex)
    {
        UIButtons.Bind(button, () => StyleSettings.SelectedThemeIndex = themeIndex);
    }

    // 연 화면으로 돌아감. 메뉴 씬이 아니면(UIManager_Lobby가 없으면) 패널만 닫음.
    private void Close()
    {
        if (UIManager_Lobby.Instance != null) UIManager_Lobby.Instance.CloseSubPanel();
        else PanelTransition.Hide(gameObject);
    }
}
