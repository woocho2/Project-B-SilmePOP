using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스크롤 목록에 글자 줄을 세로로 쌓는 공통 기능 (채팅, 접속자 목록).
// 줄 프리팹이 없으면 style 글자와 같은 글꼴·크기로 줄을 만듦. Content에 쌓는 레이아웃이 없으면 붙임.
public static class TextLines
{
    // Content가 줄 높이만큼 늘어나며 위에서부터 쌓이게 함
    public static void PrepareLayout(RectTransform content)
    {
        if (content == null) return;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 2f;
        }
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public static TMP_Text Create(RectTransform content, TMP_Text prefab, TMP_Text style, string text)
    {
        TMP_Text line;
        if (prefab != null)
        {
            line = Object.Instantiate(prefab, content);
        }
        else
        {
            GameObject go = new GameObject("Line", typeof(RectTransform));
            go.transform.SetParent(content, false);
            TextMeshProUGUI created = go.AddComponent<TextMeshProUGUI>();
            if (style != null)
            {
                created.font = style.font;
                created.fontSize = style.fontSize;
                created.color = style.color;
            }
            created.textWrappingMode = TextWrappingModes.Normal;
            created.raycastTarget = false;
            line = created;
        }

        line.text = text;
        return line;
    }

    // "닉네임#0012" → "닉네임"
    public static string Nickname(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return "";
        int tag = displayName.LastIndexOf('#');
        return tag > 0 ? displayName.Substring(0, tag) : displayName;
    }
}
