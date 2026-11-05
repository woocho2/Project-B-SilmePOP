using System;
using UnityEngine;

// 내가 고른 슬라임 색과 맵 테마 (옵션 창에서 바꿈). 씬이 바뀌어도 유지됨.
public static class StyleSettings
{
    // 값이 바뀌면 알림. MySlimeColor, ThemeSkin이 듣고 씬에 상관없이 스스로 다시 칠함.
    public static event Action<Color> ColorChanged;
    public static event Action<int> ThemeChanged;

    private static Color selectedColor = Color.white;
    private static int selectedThemeIndex;

    public static bool UseDoodlePalette { get; set; } = true;
    public static Color SelectedColor
    {
        get => selectedColor;
        set
        {
            selectedColor = value;
            ColorChanged?.Invoke(value);
        }
    }

    // 고른 맵 테마의 번호. ThemePalette의 순서 (0 = Basic).
    public static int SelectedThemeIndex
    {
        get => selectedThemeIndex;
        set
        {
            selectedThemeIndex = value;
            ThemeChanged?.Invoke(value);
        }
    }

    public const float SLIME_ALPHA = 1.0f;
}
