using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SlimeController : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private TextMeshProUGUI numberText;

    [Header("강조 (외곽선)")]
    [Tooltip("선택 사항. 비우면 몸통(Color)의 SlimeOutline 셰이더로 외곽선을 그림. 넣으면 예전처럼 이 그림을 켜고 끔")]
    [SerializeField] private GameObject highlightObject;
    private SpriteRenderer highlightRenderer;

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineUVRectId = Shader.PropertyToID("_SpriteUVRect");
    private MaterialPropertyBlock outlineBlock;
    private Sprite outlineSprite;
    private Vector4 outlineUVRect = new Vector4(0f, 0f, 1f, 1f);
    private Color currentOutlineColor = Color.clear;

    [Header("특수 슬라임 코스튬 (Costume 자식에 씌움)")]
    [SerializeField] private Sprite costumeKing;
    [SerializeField] private Sprite costumeAngel;
    [SerializeField] private Sprite costumeDevil;
    [SerializeField] private Sprite costumeJoker;

    private readonly Color hintColor = new Color(1f, 1f, 0f, 0.8f);

    // 강조는 세 가지가 겹칠 수 있고, 우선순위는 스킬 범위 > 힌트 > 드래그
    private bool isDragHighlighted;
    private Color dragColor = new Color(0.2f, 1f, 0.2f, 0.8f);
    private bool isHintActive = false;
    private bool isSkillPreview;
    private Color skillPreviewColor;

    // 합체 슬라임을 두 배로 그릴 때 되돌릴 원래 위치·크기
    private Vector3[] visualBasePositions;
    private Vector3[] visualBaseScales;

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

    // 터지는 연출 (PlayPop)
    private Vector3 baseScale;
    private Tween popTween;

    // 점수 글자(ScorePopup)를 슬라임 위에 그리기 위한 정보
    public SpriteRenderer Body => bodyRenderer;
    public TMP_FontAsset NumberFont => numberText != null ? numberText.font : null;

    private void Awake()
    {
        baseScale = transform.localScale;
        if (highlightObject != null)
        {
            highlightRenderer = highlightObject.GetComponent<SpriteRenderer>();
        }

        // 부착된 BoxCollider2D 컴포넌트 가져오기
        slimeCollider = GetComponent<BoxCollider2D>();

        bodyRenderer = transform.Find("Color")?.GetComponent<SpriteRenderer>();
        faceRenderer = transform.Find("Face")?.GetComponent<SpriteRenderer>();
        costumeRenderer = transform.Find("Costume")?.GetComponent<SpriteRenderer>();

        foreach (Transform child in transform)
        {
            if (child.gameObject != highlightObject && child.gameObject.activeSelf)
            {
                visualObjects.Add(child.gameObject);
            }
        }

        visualBasePositions = new Vector3[visualObjects.Count];
        visualBaseScales = new Vector3[visualObjects.Count];
        for (int i = 0; i < visualObjects.Count; i++)
        {
            visualBasePositions[i] = visualObjects[i].transform.localPosition;
            visualBaseScales[i] = visualObjects[i].transform.localScale;
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
        StopPop();
        bool hasSlime = slime != null;
        bool showVisuals = hasSlime && slime.Anchor == Cell;

        isDragHighlighted = false;
        isHintActive = false;
        UpdateHighlight();

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

    // 터짐: delay 뒤 살짝 부풀었다가 쏙 작아져 사라짐. 끝나면 onDone (보통 SlimeManager가 이 칸을 다시 그림).
    // 이미 판에서는 없어진 슬라임이라 바로 콜라이더를 꺼서 다음 드래그에 걸리지 않게 함.
    // 그사이 이 칸을 다시 그리면(Refresh) 연출은 멈추고 원래 크기로.
    public void PlayPop(float delay, Action onDone)
    {
        StopPop();
        if (slimeCollider != null) slimeCollider.enabled = false;
        isDragHighlighted = false;
        isHintActive = false;
        UpdateHighlight();

        popTween = DOTween.Sequence()
            .AppendInterval(delay)
            .Append(transform.DOScale(baseScale * 1.25f, 0.08f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(Vector3.zero, 0.14f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                popTween = null;
                transform.localScale = baseScale;
                onDone?.Invoke();
            })
            .SetLink(gameObject);
    }

    private void StopPop()
    {
        if (popTween == null) return;
        popTween.Kill();
        popTween = null;
        transform.localScale = baseScale;
    }

    // 종류에 맞는 코스튬. 기본·합체는 없음 (null).
    public Sprite CostumeFor(SlimeType type)
    {
        switch (type)
        {
            case SlimeType.King: return costumeKing;
            case SlimeType.Angel: return costumeAngel;
            case SlimeType.Devil: return costumeDevil;
            case SlimeType.Joker: return costumeJoker;
            default: return null;
        }
    }

    public void ApplyVisualSkin(Sprite bodySprite, Sprite costumeSprite, Color color)
    {
        if (bodyRenderer == null) return;

        if (bodySprite != null && bodyRenderer.sprite != bodySprite)
        {
            bodyRenderer.sprite = bodySprite;
            SetOutlineColor(currentOutlineColor);
        }
        bodyRenderer.color = color;

        // 판에서는 숫자가 보이도록 얼굴을 끔.
        if (faceRenderer != null) faceRenderer.enabled = false;
        if (highlightRenderer != null && bodySprite != null) highlightRenderer.sprite = bodySprite;

        if (costumeRenderer == null) return;
        costumeRenderer.sprite = costumeSprite;
        costumeRenderer.enabled = costumeRenderer.sprite != null;
        costumeRenderer.color = Color.white;
    }

    public void SetSlimeColor(Color slimeColor)
    {
        if (numberText != null)
        {
            numberText.color = GetOppositeColor(slimeColor);
        }
    }

    private Color GetOppositeColor(Color originalColor)
    {
        float luminance = (originalColor.r * 0.299f) + (originalColor.g * 0.587f) + (originalColor.b * 0.114f);
        return luminance > 0.5f ? Color.black : Color.white;
    }

    // 드래그 박스 안에 들어옴/나감
    public void SetHighlight(bool isActive)
    {
        isDragHighlighted = isActive;
        UpdateHighlight();
    }

    // 돋보기 힌트 (노랑)
    public void SetHintHighlight(bool isActive)
    {
        isHintActive = isActive;
        UpdateHighlight();
    }

    // 스킬 범위 미리보기 (가능하면 초록, 불가면 빨강). 다른 강조보다 먼저 보임.
    public void SetSkillPreview(bool isActive, Color color)
    {
        isSkillPreview = isActive;
        skillPreviewColor = color;
        UpdateHighlight();
    }

    // 드래그 중 합이 맞으면 초록, 아니면 빨강. 드래그가 시작되면 힌트 강조는 지움.
    public void SetHighlightColor(Color color)
    {
        dragColor = color;
        isHintActive = false;
        UpdateHighlight();
    }

    // 합체 슬라임의 왼쪽 아래 칸이면 그림을 네 칸 가운데(offset만큼 위·오른쪽)로 옮기고 두 배로. 아니면 원래대로.
    public void SetMergedLayout(bool isMergedAnchor, Vector2 worldOffset)
    {
        if (visualBasePositions == null) return;

        float scale = transform.lossyScale.x != 0f ? transform.lossyScale.x : 1f;
        Vector3 localOffset = isMergedAnchor ? (Vector3)(worldOffset / scale) : Vector3.zero;
        float size = isMergedAnchor ? 2f : 1f;
        for (int i = 0; i < visualObjects.Count; i++)
        {
            visualObjects[i].transform.localPosition = visualBasePositions[i] + localOffset;
            visualObjects[i].transform.localScale = visualBaseScales[i] * size;
        }
    }

    private void UpdateHighlight()
    {
        bool isOn = isSkillPreview || isHintActive || isDragHighlighted;
        Color color = isSkillPreview ? skillPreviewColor
                    : isHintActive ? hintColor
                    : dragColor;

        if (highlightObject != null)
        {
            highlightObject.SetActive(isOn);
            if (isOn && highlightRenderer != null) highlightRenderer.color = color;
            return;
        }

        SetOutlineColor(isOn ? color : Color.clear);
    }

    // 몸통의 SlimeOutline 셰이더에 외곽선 색을 넘김 (투명이면 외곽선 없음). 슬라임마다 다른 색이라 재질 대신 블록으로 넘김.
    private void SetOutlineColor(Color color)
    {
        currentOutlineColor = color;
        if (bodyRenderer == null) return;

        // textureRect는 Tight 패킹에서 사용할 수 없으므로 실제 UV에서 범위를 구한다.
        // UV 배열은 스프라이트가 바뀔 때만 읽는다.
        if (outlineSprite != bodyRenderer.sprite)
        {
            outlineSprite = bodyRenderer.sprite;
            outlineUVRect = new Vector4(0f, 0f, 1f, 1f);
            if (outlineSprite != null)
            {
                Vector2[] uv = outlineSprite.uv;
                if (uv.Length > 0)
                {
                    Vector2 min = uv[0];
                    Vector2 max = uv[0];
                    for (int i = 1; i < uv.Length; i++)
                    {
                        min = Vector2.Min(min, uv[i]);
                        max = Vector2.Max(max, uv[i]);
                    }
                    outlineUVRect = new Vector4(min.x, min.y, max.x, max.y);
                }
            }
        }

        if (outlineBlock == null) outlineBlock = new MaterialPropertyBlock();
        bodyRenderer.GetPropertyBlock(outlineBlock);
        outlineBlock.SetColor(OutlineColorId, color);
        outlineBlock.SetVector(OutlineUVRectId, outlineUVRect);
        bodyRenderer.SetPropertyBlock(outlineBlock);
    }
}
