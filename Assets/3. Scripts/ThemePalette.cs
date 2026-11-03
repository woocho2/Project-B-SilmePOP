using System;
using UnityEngine;

// 맵 테마별 색. 에셋 하나(Resources/ThemePalette)에 테마를 순서대로 담고, 맵 버튼은 번호로 고름.
[CreateAssetMenu(fileName = "ThemePalette", menuName = "SlimeGame/Theme Palette")]
public class ThemePalette : ScriptableObject
{
    [Serializable]
    public class Theme
    {
        public string themeName;
        public Color CoverColor = Color.white;
        public Color PaperColor = Color.white;
        public Color SliderFillColor = Color.white;
    }

    // 맵 버튼과 같은 순서: 0 Basic, 1 Spring, 2 Summer, 3 Autumn, 4 Winter, 5 Sunny, 6 Night, 7 Cloudy
    public Theme[] themes = new Theme[0];

    private static ThemePalette loaded;

    // Resources 폴더의 ThemePalette 에셋. 없으면 null.
    public static ThemePalette Instance
    {
        get
        {
            if (loaded == null) loaded = Resources.Load<ThemePalette>("ThemePalette");
            return loaded;
        }
    }

    public Theme Get(int index)
    {
        return themes != null && index >= 0 && index < themes.Length ? themes[index] : null;
    }
}
