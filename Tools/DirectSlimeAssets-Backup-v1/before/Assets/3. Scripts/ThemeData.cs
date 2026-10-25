using UnityEngine;

[CreateAssetMenu(fileName = "ThemeData", menuName = "SlimeGame/Map Theme Data")]
public class ThemeData : ScriptableObject
{
    [Header("맵 기본 정보")]
    public string themeName;

    [Header("맵 UI 및 배경 스프라이트")]
    public Sprite MenuSprite;
    public Sprite TableSprite;
    public Sprite SlimeSprite;
}