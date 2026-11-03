using System;
using UnityEngine;

public static class ChangeData
{
    // 값이 바뀌면 알림. Change_Slime, Change_Theme이 듣고 씬에 상관없이 스스로 다시 칠함.
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
