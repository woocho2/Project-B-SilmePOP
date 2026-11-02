using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SlimeController : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private TextMeshProUGUI numberText;

    [Header("Highlight Component")]
    [SerializeField] private GameObject highlightObject;
    private SpriteRenderer highlightRenderer;

    private readonly Color hintColor = new Color(1f, 1f, 0f, 0.8f);
    private readonly Color destroyHoverColor = new Color(0.2f, 0.5f, 1f, 0.8f); // 파괴 스킬용 파란색 추가

    private bool isHintActive = false;
    private bool isDestroyHoverActive = false; // 파괴 호버 상태 추적용 변수 추가

    public int Cell { get; private set; }
    public int CurrentNumber { get; private set; }
    public SlimeType CurrentType { get; private set; }

    // 콜라이더 제어용 변수 추가
    private BoxCollider2D slimeCollider;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer faceRenderer;
    private SpriteRenderer costumeRenderer;

    // 슬라임이 없는 칸에서 끄는 그림들 (강조는 따로 관리)
    private readonly List<GameObject> visualObjects = new List<GameObject>();

    private void Awake()
    {
        if (highlightObject != null)
        {
            highlightRenderer = highlightObject.GetComponent<SpriteRenderer>();
        }

        // 부착된 BoxCollider2D 컴포넌트 가져오기
        slimeCollider = GetComponent<BoxCollider2D>();

        bodyRenderer = transform.Find("Color")?.GetComponent<SpriteRenderer>();
        faceRenderer = transform.Find("Face")?.GetComponent<SpriteRenderer>();
        costumeRenderer = transform.Find("Costume")?.GetComponent<SpriteRenderer>();
        if (bodyRenderer != null && costumeRenderer == null)
        {
            var costumeObject = new GameObject("Costume", typeof(SpriteRenderer));
            costumeObject.transform.SetParent(transform, false);
            costumeObject.layer = bodyRenderer.gameObject.layer;
            costumeRenderer = costumeObject.GetComponent<SpriteRenderer>();
        }

        foreach (Transform child in transform)
        {
            if (child.gameObject != highlightObject && child.gameObject.activeSelf)
            {
                visualObjects.Add(child.gameObject);
            }
        }
    }

    private void Start()
    {
        DoodleLabSkin.ApplySlime(this);
        SetHighlight(false);
    }

    public void Init(int cell)
    {
        Cell = cell;
    }

    // 이 칸의 슬라임을 그림 (빈칸이면 null).
    // 합체 슬라임의 나머지 세 칸은 그림만 끄고 콜라이더는 켜 둠: 한 칸만 걸쳐도 포함되어야 함 (기획 [4-1] 4번).
    public void Refresh(BoardSlime slime)
    {
        bool hasSlime = slime != null;
        bool showVisuals = hasSlime && slime.Anchor == Cell;

        isHintActive = false;
        isDestroyHoverActive = false;
        if (highlightObject != null) highlightObject.SetActive(false);

        if (hasSlime)
        {
            CurrentNumber = slime.Number;
            CurrentType = slime.Type;
            if (numberText != null) numberText.text = CurrentNumber.ToString();
        }

        foreach (GameObject visual in visualObjects)
        {
            visual.SetActive(showVisuals);
        }

        if (slimeCollider != null) slimeCollider.enabled = hasSlime;
        if (showVisuals) DoodleLabSkin.ApplySlime(this);
    }

    public void ApplyVisualSkin(Sprite bodySprite, Sprite costumeSprite, Sprite jokerSprite, Color color)
    {
        if (bodyRenderer == null) return;

        bool isJoker = CurrentType == SlimeType.Joker;
        Sprite sprite = isJoker ? jokerSprite : bodySprite;
        if (sprite != null) bodyRenderer.sprite = sprite;
        bodyRenderer.color = isJoker ? Color.white : color;

        // Gameplay keeps its center clear for the number. UI faces are handled by Change.
        if (faceRenderer != null) faceRenderer.enabled = false;
        if (highlightRenderer != null && bodySprite != null) highlightRenderer.sprite = bodySprite;

        if (costumeRenderer == null) return;
        costumeRenderer.sprite = isJoker ? null : costumeSprite;
        costumeRenderer.enabled = costumeRenderer.sprite != null;
        costumeRenderer.color = Color.white;
        costumeRenderer.sharedMaterial = bodyRenderer.sharedMaterial;
        costumeRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        costumeRenderer.sortingOrder = bodyRenderer.sortingOrder + 1;
        costumeRenderer.flipX = bodyRenderer.flipX;
        costumeRenderer.flipY = bodyRenderer.flipY;
        costumeRenderer.transform.localPosition = bodyRenderer.transform.localPosition;
        costumeRenderer.transform.localRotation = bodyRenderer.transform.localRotation;
        costumeRenderer.transform.localScale = bodyRenderer.transform.localScale;
    }

    public void SetSlimeColor(Color slimeColor)
    {
        if (numberText != null)
        {
            Color oppositeColor = CurrentType == SlimeType.Joker ? Color.white : GetOppositeColor(slimeColor);
            numberText.color = oppositeColor;
            DoodleLabSkin.StyleSlimeNumber(numberText);
        }
    }

    private Color GetOppositeColor(Color originalColor)
    {
        float luminance = (originalColor.r * 0.299f) + (originalColor.g * 0.587f) + (originalColor.b * 0.114f);
        return luminance > 0.5f ? Color.black : Color.white;
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(isActive || isHintActive || isDestroyHoverActive);
        }
    }

    public void SetHintHighlight(bool isActive)
    {
        isHintActive = isActive;

        if (highlightObject != null && highlightRenderer != null)
        {
            // 파괴 스킬 호버 상태가 아닐 때만 힌트 색상을 적용 (호버가 우선순위 높음)
            if (!isDestroyHoverActive)
            {
                highlightObject.SetActive(isActive);
                if (isActive)
                {
                    highlightRenderer.color = hintColor;
                }
            }
        }
    }

    /// <summary>
    /// 파괴 스킬 사용 중 마우스 호버에 따른 파란색 하이라이트를 제어합니다.
    /// </summary>
    public void SetDestroyHoverHighlight(bool isActive)
    {
        isDestroyHoverActive = isActive;

        if (highlightObject != null && highlightRenderer != null)
        {
            if (isActive)
            {
                highlightObject.SetActive(true);
                highlightRenderer.color = destroyHoverColor; // 파란색 적용
            }
            else
            {
                // 호버가 꺼졌을 때, 힌트 상태였다면 기존 노란색으로 복구
                if (isHintActive)
                {
                    highlightObject.SetActive(true);
                    highlightRenderer.color = hintColor;
                }
                else
                {
                    highlightObject.SetActive(false);
                }
            }
        }
    }

    public void SetHighlightColor(Color color)
    {
        if (highlightRenderer != null)
        {
            highlightRenderer.color = color;
            isHintActive = false;
            isDestroyHoverActive = false;
        }
    }

    public void SetColliderSize(float sizeX, float sizeY)
    {
        if (slimeCollider != null)
        {
            slimeCollider.size = new Vector2(sizeX, sizeY);
        }
    }
}
