using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 결과 화면의 작은 최종 판. 검은 칸(area) 안에 17 x 10 판을 맞춰 넣고, 남은 슬라임을 동그라미 + 숫자로 그림.
// 기본 슬라임은 그 플레이어의 색, 특수 슬라임은 종류마다 정해진 색. 합체 슬라임은 2 x 2 크기.
public static class FinalBoardView
{
    private const string ContainerName = "FinalBoard";
    private const float Padding = 0.04f;      // 검은 칸 가장자리 여백 (짧은 변의 비율)
    private const float DotScale = 0.9f;      // 칸 대비 동그라미 크기
    private const float NumberScale = 0.62f;  // 칸 대비 숫자 크기

    private static readonly Color DevilColor = new Color(0.55f, 0.25f, 0.65f);
    private static readonly Color AngelColor = new Color(1f, 0.96f, 0.75f);
    private static readonly Color KingColor = new Color(1f, 0.78f, 0.2f);
    private static readonly Color JokerColor = new Color(1f, 0.45f, 0.75f);
    private static readonly Color MergedColor = new Color(0.30f, 0.50f, 0.95f);

    private static Sprite dotSprite;

    // 이전에 그린 것을 지움 (판이 없으면 빈 검은 칸)
    public static void Clear(RectTransform area)
    {
        if (area == null) return;

        // Destroy는 프레임 끝에 지워지므로, 같은 프레임에 다시 그려도 헷갈리지 않게 이름을 바꾸고 숨긴 뒤 지움
        for (int i = area.childCount - 1; i >= 0; i--)
        {
            Transform child = area.GetChild(i);
            if (child.name != ContainerName) continue;
            child.name = ContainerName + " (old)";
            child.gameObject.SetActive(false);
            Object.Destroy(child.gameObject);
        }
    }

    public static void Draw(RectTransform area, BoardSnapshot snapshot, Color slimeColor, TMP_FontAsset font)
    {
        Clear(area);
        if (area == null || snapshot == null || snapshot.numbers == null || snapshot.anchors == null) return;

        RectTransform container = new GameObject(ContainerName, typeof(RectTransform)).GetComponent<RectTransform>();
        container.SetParent(area, false);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;

        Rect rect = area.rect;
        float pad = Mathf.Min(rect.width, rect.height) * Padding;
        float cell = Mathf.Min((rect.width - pad * 2f) / GameRules.Columns, (rect.height - pad * 2f) / GameRules.Rows);
        Vector2 origin = new Vector2((rect.width - cell * GameRules.Columns) / 2f, (rect.height - cell * GameRules.Rows) / 2f);

        for (int anchor = 0; anchor < GameRules.CellCount && anchor < snapshot.anchors.Length; anchor++)
        {
            if (snapshot.anchors[anchor] != anchor) continue;   // 슬라임마다 Anchor 칸에서 한 번만 그림

            // 이 슬라임이 차지한 칸들 (합체는 2 x 2)
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int c = 0; c < snapshot.anchors.Length && c < GameRules.CellCount; c++)
            {
                if (snapshot.anchors[c] != anchor) continue;
                int x = GameRules.IndexToX(c);
                int y = GameRules.IndexToY(c);
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }

            SlimeType type = snapshot.types != null && anchor < snapshot.types.Length ? (SlimeType)snapshot.types[anchor] : SlimeType.Basic;
            Color color = ColorFor(type, slimeColor);
            float size = (maxX - minX + 1) * cell;
            Vector2 center = origin + new Vector2((minX + maxX + 1) * 0.5f * cell, (minY + maxY + 1) * 0.5f * cell);

            RectTransform dot = CreateDot(container, center, size * DotScale, color);
            CreateNumber(dot, snapshot.numbers[anchor], size * NumberScale, color, font);
        }
    }

    private static Color ColorFor(SlimeType type, Color slimeColor)
    {
        switch (type)
        {
            case SlimeType.Devil: return DevilColor;
            case SlimeType.Angel: return AngelColor;
            case SlimeType.King: return KingColor;
            case SlimeType.Joker: return JokerColor;
            case SlimeType.Merged: return MergedColor;
            default: return slimeColor;
        }
    }

    private static RectTransform CreateDot(RectTransform parent, Vector2 center, float size, Color color)
    {
        RectTransform dot = new GameObject("Slime", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        dot.SetParent(parent, false);
        dot.anchorMin = Vector2.zero;
        dot.anchorMax = Vector2.zero;
        dot.pivot = new Vector2(0.5f, 0.5f);
        dot.anchoredPosition = center;
        dot.sizeDelta = new Vector2(size, size);

        Image image = dot.GetComponent<Image>();
        image.sprite = DotSprite();
        image.color = color;
        image.raycastTarget = false;
        return dot;
    }

    private static void CreateNumber(RectTransform dot, int number, float fontSize, Color background, TMP_FontAsset font)
    {
        if (number <= 0) return;

        TextMeshProUGUI text = new GameObject("Number", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        RectTransform rect = text.rectTransform;
        rect.SetParent(dot, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        if (font != null) text.font = font;
        text.text = number.ToString();
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        float luminance = background.r * 0.299f + background.g * 0.587f + background.b * 0.114f;
        text.color = luminance > 0.5f ? Color.black : Color.white;
    }

    // 동그라미 그림 (그림 파일 없이 한 번 만들어 계속 씀)
    private static Sprite DotSprite()
    {
        if (dotSprite != null) return dotSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Color32[] pixels = new Color32[size * size];
        float radius = size / 2f - 1f;
        Vector2 middle = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(x, y), middle) + 0.5f);   // 가장자리를 부드럽게
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        dotSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return dotSprite;
    }
}
