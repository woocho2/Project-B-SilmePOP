using UnityEngine;

public static class ChangeData
{
    public static Color SelectedColor { get; set; } = Color.white;
    public static Sprite SelectedFace { get; set; }
    public static Sprite SelectedCostume { get; set; }
    public static Color LastCustomColor { get; set; } = Color.white;

    public static ThemeData SelectedMapTheme { get; set; }

    public const float SLIME_ALPHA = 1.0f;
    public const string HEX_WHITE = "#FFFFFF";
    public const string HEX_BLACK = "#383838";
    public const string HEX_RED = "#B3270B";
    public const string HEX_BLUE = "#257BD6";
    public const string HEX_YELLOW = "#DBD526";
    public const string HEX_GREEN = "#02BE00";
    public const string HEX_YELLOWGREEN = "#B5FF00";
    public const string HEX_BROWN = "#733F01";
}