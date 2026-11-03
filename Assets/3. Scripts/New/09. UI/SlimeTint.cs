using UnityEngine;
using UnityEngine.UI;

// 슬라임 오브젝트의 Base를 정해진 색으로 칠함. 다른 사람(방장, 대기실의 상대) 슬라임용.
// 슬라임 프리팹에 붙은 Change_Slime은 "내" 색을 칠하므로 여기서는 끔.
public sealed class SlimeTint
{
    private const string BaseName = "Base";

    private readonly Image baseImage;
    private readonly SpriteRenderer baseRenderer;

    public SlimeTint(GameObject slime)
    {
        if (slime == null) return;

        foreach (Change_Slime mine in slime.GetComponentsInChildren<Change_Slime>(true))
        {
            mine.enabled = false;
        }

        foreach (Transform child in slime.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != BaseName) continue;
            baseImage = child.GetComponent<Image>();
            baseRenderer = child.GetComponent<SpriteRenderer>();
            break;
        }
    }

    // "#RRGGBB". 읽을 수 없는 값이면 그대로 둠.
    public void Apply(string colorHex)
    {
        if (!ColorUtility.TryParseHtmlString(colorHex, out Color color)) return;

        if (baseImage != null) baseImage.color = color;
        if (baseRenderer != null) baseRenderer.color = color;
    }
}
