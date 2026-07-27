using UnityEngine;

public static class ChangeData
{
    // 슬라임 외형 데이터
    public static Color SelectedColor { get; set; } = Color.white;
    public static Sprite SelectedFace { get; set; }

    public static Color LastCustomColor { get; set; } = Color.white;

    // 현재 선택된 맵 테마 SO 데이터
    public static ThemeData SelectedMapTheme { get; set; }
    
    // 색상 헥사코드 정의
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