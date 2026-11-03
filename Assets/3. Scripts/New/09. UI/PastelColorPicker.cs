using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 고정 10색 물감 팔레트. 편집기에 배치한 Image/Button만 사용하며 UI를 생성하지 않음.
public sealed class PastelColorPicker : MonoBehaviour
{
    [Serializable]
    private sealed class Swatch
    {
        public string displayName;
        public Color color = Color.white;
        public Button button;
        public Image paint;
        public GameObject selectedMark;
    }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private RectTransform paper;
    [SerializeField] private CanvasGroup popupCanvas;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backdropButton;
    [SerializeField, Min(0f)] private float openDuration = 0.18f;
    [SerializeField, Min(0f)] private float closeDuration = 0.12f;

    [Header("Selected Color")]
    [Tooltip("얼굴을 제외한 미리보기 슬라임 본체 Image")]
    [SerializeField] private Image previewBody;
    [SerializeField] private TMP_Text selectedColorName;

    [Header("Paint Colors")]
    [SerializeField] private Swatch[] swatches = new Swatch[0];

    private UnityAction[] pickActions;
    private Sequence popupTween;
    private Vector3 paperScale;
    private GameObject previousSelection;
    private bool isOpen;
    private Color pendingColor;

    private void Awake()
    {
        paperScale = paper != null ? paper.localScale : Vector3.one;
        pickActions = new UnityAction[swatches.Length];
        for (int i = 0; i < swatches.Length; i++)
        {
            int index = i;
            pickActions[i] = () => SelectColor(index);
        }
    }

    private void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (applyButton != null) applyButton.onClick.AddListener(Apply);
        if (backdropButton != null) backdropButton.onClick.AddListener(Close);

        for (int i = 0; i < swatches.Length; i++)
        {
            Swatch swatch = swatches[i];
            if (swatch == null) continue;
            if (swatch.paint != null) swatch.paint.color = Opaque(swatch.color);
            if (swatch.button != null) swatch.button.onClick.AddListener(pickActions[i]);
        }

        ChangeData.ColorChanged += OnAppliedColorChanged;
        HideImmediately();
        RefreshSelection(ChangeData.SelectedColor);
    }

    private void OnDisable()
    {
        ChangeData.ColorChanged -= OnAppliedColorChanged;
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (applyButton != null) applyButton.onClick.RemoveListener(Apply);
        if (backdropButton != null) backdropButton.onClick.RemoveListener(Close);
        for (int i = 0; i < swatches.Length; i++)
        {
            if (swatches[i]?.button != null)
                swatches[i].button.onClick.RemoveListener(pickActions[i]);
        }
        HideImmediately();
    }

    private void Update()
    {
        if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    // OptionPanel의 Color / Slime 버튼에서 호출.
    public void Open()
    {
        if (isOpen || !isActiveAndEnabled || popupRoot == null) return;
        bool wasVisible = popupRoot.activeSelf;
        if (!wasVisible && EventSystem.current != null)
            previousSelection = EventSystem.current.currentSelectedGameObject;

        popupTween?.Kill();
        popupRoot.SetActive(true);
        isOpen = true;
        RefreshSelection(ChangeData.SelectedColor);

        if (!wasVisible)
        {
            if (popupCanvas != null) popupCanvas.alpha = 0f;
            if (paper != null) paper.localScale = paperScale * 0.94f;
        }
        if (popupCanvas != null)
        {
            popupCanvas.interactable = true;
            popupCanvas.blocksRaycasts = true;
        }

        popupTween = DOTween.Sequence().SetUpdate(true);
        if (popupCanvas != null)
            popupTween.Join(DOTween.To(() => popupCanvas.alpha,
                value => popupCanvas.alpha = value, 1f, openDuration));
        if (paper != null)
            popupTween.Join(paper.DOScale(paperScale, openDuration).SetEase(Ease.OutBack));

        FocusSelectedColor();
    }

    // 미리보기 중인 색은 적용 버튼을 눌렀을 때만 게임에 전달.
    public void Apply()
    {
        if (!isOpen) return;
        ChangeData.UseDoodlePalette = false;
        ChangeData.SelectedColor = pendingColor;
        Close();
    }

    public void Close()
    {
        if (!isOpen) return;
        SoundManager.Play(SoundEffect.Click);
        isOpen = false;
        RefreshSelection(ChangeData.SelectedColor);
        popupTween?.Kill();
        if (popupCanvas != null) popupCanvas.interactable = false;

        popupTween = DOTween.Sequence().SetUpdate(true);
        if (popupCanvas != null)
            popupTween.Join(DOTween.To(() => popupCanvas.alpha,
                value => popupCanvas.alpha = value, 0f, closeDuration));
        if (paper != null)
            popupTween.Join(paper.DOScale(paperScale * 0.97f, closeDuration).SetEase(Ease.InQuad));
        popupTween.OnComplete(HideImmediately);
    }

    public void HideImmediately()
    {
        isOpen = false;
        popupTween?.Kill();
        popupTween = null;
        if (popupCanvas != null)
        {
            popupCanvas.alpha = 0f;
            popupCanvas.interactable = false;
            popupCanvas.blocksRaycasts = false;
        }
        if (paper != null) paper.localScale = paperScale;
        RestoreFocus();
        if (popupRoot != null && popupRoot != gameObject) popupRoot.SetActive(false);
        RefreshSelection(ChangeData.SelectedColor);
    }

    private void SelectColor(int index)
    {
        if (!isOpen || index < 0 || index >= swatches.Length || swatches[index] == null) return;
        SoundManager.Play(SoundEffect.Click);
        RefreshSelection(swatches[index].color);
    }

    private void OnAppliedColorChanged(Color color)
    {
        // 외부 색상 갱신이 편집 중인 미리보기를 덮어쓰지 않음.
        if (!isOpen) RefreshSelection(color);
    }

    private void RefreshSelection(Color selected)
    {
        selected = Opaque(selected);
        pendingColor = selected;
        if (previewBody != null) previewBody.color = selected;

        string colorName = "현재 색상";
        foreach (Swatch swatch in swatches)
        {
            if (swatch == null) continue;
            bool matches = SameColor(selected, swatch.color);
            if (swatch.selectedMark != null) swatch.selectedMark.SetActive(matches);
            if (matches) colorName = swatch.displayName;
        }
        if (selectedColorName != null) selectedColorName.text = colorName;
    }

    private void FocusSelectedColor()
    {
        if (EventSystem.current == null) return;
        Button first = null;
        foreach (Swatch swatch in swatches)
        {
            if (swatch?.button == null) continue;
            if (first == null) first = swatch.button;
            if (!SameColor(ChangeData.SelectedColor, swatch.color)) continue;
            EventSystem.current.SetSelectedGameObject(swatch.button.gameObject);
            return;
        }
        if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    private void RestoreFocus()
    {
        if (EventSystem.current != null && previousSelection != null &&
            previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        previousSelection = null;
    }

    private static Color Opaque(Color color)
    {
        color.a = ChangeData.SLIME_ALPHA;
        return color;
    }

    private static bool SameColor(Color a, Color b)
    {
        const float tolerance = 0.005f;
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }
}
