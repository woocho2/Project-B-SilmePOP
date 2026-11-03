using UnityEngine;
using UnityEngine.UI;

// 맵 테마에 따라 이 오브젝트의 Image 또는 SpriteRenderer를 바꿈. 바뀌어야 할 오브젝트에 직접 붙임.
// 색: Part를 고르면 ThemePalette의 그 테마 색으로 칠함 (None이면 색은 그대로).
// 그림: Theme Sprites에 테마 순서대로 그림을 넣으면 그 테마일 때 그 그림으로 바꿈 (비운 칸은 그림 그대로).
public class Change_Theme : MonoBehaviour
{
    public enum Part
    {
        Cover = 0,
        Paper = 1,
        SliderFill = 2,
        None = 3        // 색은 바꾸지 않음 (그림만 바꿀 때)
    }

    [SerializeField] private Part part;
    [Tooltip("테마별 그림. ThemePalette와 같은 순서: 0 Basic, 1 Spring, 2 Summer, 3 Autumn, 4 Winter, 5 Sunny, 6 Night, 7 Cloudy")]
    [SerializeField] private Sprite[] themeSprites = new Sprite[0];

    private Image targetImage;
    private SpriteRenderer targetRenderer;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        targetRenderer = GetComponent<SpriteRenderer>();

        if (targetImage == null && targetRenderer == null)
        {
            Debug.LogWarning($"Change_Theme: {name}에 Image나 SpriteRenderer가 없습니다.", this);
        }
        if (part != Part.None && ThemePalette.Instance == null)
        {
            Debug.LogWarning("Change_Theme: Resources 폴더에 ThemePalette 에셋이 없습니다.", this);
        }
    }

    private void OnEnable()
    {
        ChangeData.ThemeChanged += ApplyTheme;
        ApplyTheme(ChangeData.SelectedThemeIndex);
    }

    private void OnDisable()
    {
        ChangeData.ThemeChanged -= ApplyTheme;
    }

    private void ApplyTheme(int themeIndex)
    {
        ApplySprite(themeIndex);
        ApplyColor(themeIndex);
    }

    private void ApplySprite(int themeIndex)
    {
        if (themeIndex < 0 || themeIndex >= themeSprites.Length) return;

        Sprite sprite = themeSprites[themeIndex];
        if (sprite == null) return;

        if (targetImage != null) targetImage.sprite = sprite;
        if (targetRenderer != null) targetRenderer.sprite = sprite;
    }

    private void ApplyColor(int themeIndex)
    {
        if (part == Part.None) return;

        ThemePalette palette = ThemePalette.Instance;
        ThemePalette.Theme theme = palette != null ? palette.Get(themeIndex) : null;
        if (theme == null) return;

        Color color = part switch
        {
            Part.Cover => theme.CoverColor,
            Part.Paper => theme.PaperColor,
            _ => theme.SliderFillColor
        };

        if (targetImage != null) targetImage.color = color;
        if (targetRenderer != null) targetRenderer.color = color;
    }
}
