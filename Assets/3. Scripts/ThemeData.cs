using UnityEngine;

[CreateAssetMenu(fileName = "ThemeData", menuName = "SlimeGame/Map Theme Data")]
public class ThemeData : ScriptableObject
{
    [Header("맵 기본 정보")]
    public string themeName; // 테마 이름 (예: Basic, Spring, Summer 등)

    [Header("맵 UI 및 배경 스프라이트")]
    public Sprite MenuSprite;       // 메인 메뉴 배경
    public Sprite TableSprite; // 옵션 배경 및 게임 테이블
    public Sprite SlimeSprite;
}