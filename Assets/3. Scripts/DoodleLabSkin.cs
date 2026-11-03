using System.Collections.Generic;
using UnityEngine;

/// <summary>Skin lookup and visual state changes. Scene objects and layout are authored in the Editor.</summary>
public static class DoodleLabSkin
{
    private static readonly Color[] Palette =
    {
        new Color(0.72f, 0.95f, 0.34f), new Color(0.76f, 0.97f, 0.39f),
        new Color(0.69f, 0.92f, 0.31f), new Color(0.79f, 0.97f, 0.43f)
    };
    private static Dictionary<string, Sprite> sprites;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => sprites = null;

    public static void Register(DoodleSkinAssets assets)
    {
        sprites = new Dictionary<string, Sprite>();
        if (assets == null || assets.sprites == null) return;
        foreach (var binding in assets.sprites)
            if (!string.IsNullOrEmpty(binding.name) && binding.sprite != null)
                sprites[binding.name] = binding.sprite;
    }

    public static Sprite Sprite(string name) =>
        sprites != null && sprites.TryGetValue(name, out var value) ? value : null;

    public static Color SlimeColor(Color selected, int number, SlimeType type = SlimeType.Basic)
    {
        switch (type)
        {
            case SlimeType.Merged: return new Color(0.30f, 0.50f, 0.95f);
        }
        if (!ChangeData.UseDoodlePalette) return selected;
        return Palette[Mathf.Max(0, number - 1) % Palette.Length];
    }

    public static void ApplySlime(SlimeController slime)
    {
        if (slime == null) return;
        Color color = SlimeColor(ChangeData.SelectedColor, slime.CurrentNumber, slime.CurrentType);
        // 코스튬과 몸통 그림은 슬라임 프리팹에 넣은 것을 씀 (몸통이 null이면 프리팹 그림 그대로)
        slime.ApplyVisualSkin(Sprite("Body"), slime.CostumeFor(slime.CurrentType), color);
        slime.SetSlimeColor(color);
    }
}
