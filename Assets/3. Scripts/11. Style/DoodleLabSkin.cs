using UnityEngine;

// 판 위 슬라임의 색과 그림. 그림(몸통·코스튬)은 슬라임 프리팹에 넣은 것을 씀.
public static class DoodleLabSkin
{
    private static readonly Color[] Palette =
    {
        new Color(0.72f, 0.95f, 0.34f), new Color(0.76f, 0.97f, 0.39f),
        new Color(0.69f, 0.92f, 0.31f), new Color(0.79f, 0.97f, 0.43f)
    };

    public static Color SlimeColor(Color selected, int number, SlimeType type = SlimeType.Basic)
    {
        switch (type)
        {
            case SlimeType.Merged: return new Color(0.30f, 0.50f, 0.95f);
        }
        if (!StyleSettings.UseDoodlePalette) return selected;
        return Palette[Mathf.Max(0, number - 1) % Palette.Length];
    }

    public static void ApplySlime(SlimeController slime)
    {
        if (slime == null) return;
        Color color = SlimeColor(StyleSettings.SelectedColor, slime.CurrentNumber, slime.CurrentType);
        // 몸통 그림은 프리팹 그대로(null), 코스튬은 종류에 맞는 것
        slime.ApplyVisualSkin(null, slime.CostumeFor(slime.CurrentType), color);
        slime.SetSlimeColor(color);
    }
}
