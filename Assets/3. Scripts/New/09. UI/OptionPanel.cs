using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 옵션 패널: BGM·SFX 볼륨, 슬라임 색(컬러피커), 맵 테마, 닫기. OptionPanel(프리팹)에 붙임.
// 볼륨 슬라이더의 Fill 색은 Change_Theme, 슬라임 색은 Change_Slime이 맡음.
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

    private void Awake()
    {
        AudioVolumeSlider.Bind(slider_bgmVolume, AudioVolumeSlider.VolumeChannel.BGM, txt_bgmValue);
        AudioVolumeSlider.Bind(slider_sfxVolume, AudioVolumeSlider.VolumeChannel.SFX, txt_sfxValue);

        UIButtons.Bind(btn_slimeColor, () => { if (slimeColorPicker != null) slimeColorPicker.Open(); });

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

    private static void BindMap(Button button, int themeIndex)
    {
        UIButtons.Bind(button, () => ChangeData.SelectedThemeIndex = themeIndex);
    }

    // 연 화면으로 돌아감. 메뉴 씬이 아니면(UIManager_Menu가 없으면) 패널만 닫음.
    private void Close()
    {
        if (UIManager_Menu.Instance != null) UIManager_Menu.Instance.CloseSubPanel();
        else gameObject.SetActive(false);
    }
}
