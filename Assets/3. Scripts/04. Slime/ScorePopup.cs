using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

// 판 위에 잠깐 떠오르는 "+12" 글자. 톡 커지며 나타나 위로 떠오르다 사라짐.
// 점수가 클수록(킹·조커·천사) 크고 노랗게, 0점은 회색, 손해는 빨갛게.
// 월드 공간 글자(TextMeshPro)라 캔버스가 필요 없음. 다 쓴 글자는 모아 두었다가 다시 씀.
public static class ScorePopup
{
    private const float Rise = 0.7f;          // 떠오르는 높이 (월드 단위)
    private const float Duration = 0.8f;
    private const int BigScore = 15;          // 이 이상이면 크고 노랗게

    private static readonly Color NormalColor = new Color(1f, 0.55f, 0.1f);    // 주황
    private static readonly Color BigColor = new Color(1f, 0.82f, 0.1f);       // 금색
    private static readonly Color ZeroColor = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color MinusColor = new Color(0.9f, 0.2f, 0.2f);

    private static readonly Stack<TextMeshPro> pool = new Stack<TextMeshPro>();

    // sortingSource: 슬라임 몸통 렌더러. 같은 정렬 레이어의 맨 위에 그림
    public static void Show(Vector2 position, int score, float size, TMP_FontAsset font, Renderer sortingSource)
    {
        TextMeshPro text = Take(font);
        if (text == null) return;

        bool big = score >= BigScore;
        text.text = score > 0 ? $"+{score}" : score.ToString();
        text.color = score < 0 ? MinusColor : score == 0 ? ZeroColor : big ? BigColor : NormalColor;
        text.fontSize = big ? size * 1.4f : size;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(60, 40, 20, 255);

        MeshRenderer renderer = text.GetComponent<MeshRenderer>();
        if (sortingSource != null) renderer.sortingLayerID = sortingSource.sortingLayerID;
        renderer.sortingOrder = 100;

        Transform t = text.transform;
        t.position = new Vector3(position.x, position.y, 0f);
        t.localScale = Vector3.one * 0.4f;

        DOTween.Sequence()
            .Append(t.DOScale(Vector3.one * (big ? 1.25f : 1f), 0.18f).SetEase(Ease.OutBack))
            .Join(t.DOMoveY(position.y + Rise, Duration).SetEase(Ease.OutCubic))
            .Insert(Duration * 0.55f, DOTween.ToAlpha(() => text.color, c => text.color = c, 0f, Duration * 0.45f))
            .OnComplete(() => Return(text))
            .SetLink(text.gameObject);
    }

    private static TextMeshPro Take(TMP_FontAsset font)
    {
        while (pool.Count > 0)
        {
            TextMeshPro pooled = pool.Pop();
            if (pooled == null) continue;   // 씬이 바뀌어 지워짐
            pooled.gameObject.SetActive(true);
            if (font != null && pooled.font != font) pooled.font = font;
            return pooled;
        }

        TextMeshPro text = new GameObject("ScorePopup").AddComponent<TextMeshPro>();
        if (font != null) text.font = font;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(4f, 1.5f);
        return text;
    }

    private static void Return(TextMeshPro text)
    {
        if (text == null) return;
        text.gameObject.SetActive(false);
        pool.Push(text);
    }
}
