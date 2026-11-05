using UnityEngine;
using UnityEngine.UI;

// 붙어 있는 오브젝트의 Image 또는 SpriteRenderer를 고른 색으로 칠함.
// 부모에 붙였다면 자식 중 이름이 Base인 오브젝트의 그림을 사용함.
public class MySlimeColor : MonoBehaviour
{
    private const string BaseName = "Base";

    private Image baseImage;
    private SpriteRenderer baseRenderer;

    private void Awake()
    {
        baseImage = GetComponent<Image>();
        baseRenderer = GetComponent<SpriteRenderer>();
        if (baseImage != null || baseRenderer != null) return;

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform || child.name != BaseName) continue;

            baseImage = child.GetComponent<Image>();
            baseRenderer = child.GetComponent<SpriteRenderer>();
            if (baseImage != null || baseRenderer != null) break;
        }

        if (baseImage == null && baseRenderer == null)
        {
            Debug.LogWarning($"MySlimeColor: {name} 자체 또는 자식 Base에 Image나 SpriteRenderer가 없습니다.", this);
        }
    }

    private void OnEnable()
    {
        StyleSettings.ColorChanged += ApplyColor;
        ApplyColor(StyleSettings.SelectedColor);
    }

    private void OnDisable()
    {
        StyleSettings.ColorChanged -= ApplyColor;
    }

    private void ApplyColor(Color color)
    {
        if (baseImage != null) baseImage.color = color;
        if (baseRenderer != null) baseRenderer.color = color;
    }
}
