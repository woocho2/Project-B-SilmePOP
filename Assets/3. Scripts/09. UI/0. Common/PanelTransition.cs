using DG.Tweening;
using UnityEngine;

// 패널을 부드럽게 켜고 끔. 켤 때는 투명 → 불투명으로 살짝 커지며 나타나고, 끌 때는 반대로 사라진 뒤 꺼짐.
// 패널에 미리 붙일 필요 없음. Show/Hide를 처음 부를 때 CanvasGroup과 함께 저절로 붙음.
// 사라지는 동안에는 클릭을 받지 않음. 시간이 멈춰도(Time.timeScale = 0) 움직임.
public class PanelTransition : MonoBehaviour
{
    private const float ShowDuration = 0.2f;
    private const float HideDuration = 0.15f;
    private const float ShowFromScale = 0.96f;
    private const float HideToScale = 0.97f;

    private CanvasGroup group;
    private Vector3 baseScale;
    private Sequence tween;
    private bool isHiding;

    public static void Show(GameObject panel)
    {
        if (panel != null) Get(panel).PlayShow();
    }

    public static void Hide(GameObject panel)
    {
        if (panel != null && panel.activeSelf) Get(panel).PlayHide();
    }

    // 애니메이션 없이 바로 켜거나 끔 (씬 시작 때 처음 모습 맞추기)
    public static void SetImmediate(GameObject panel, bool visible)
    {
        if (panel == null) return;
        if (panel.TryGetComponent(out PanelTransition transition)) transition.StopAndReset();
        panel.SetActive(visible);
    }

    // 켜져 있고 사라지는 중도 아님
    public static bool IsShown(GameObject panel)
    {
        if (panel == null || !panel.activeSelf) return false;
        return !(panel.TryGetComponent(out PanelTransition transition) && transition.isHiding);
    }

    private static PanelTransition Get(GameObject panel)
    {
        if (!panel.TryGetComponent(out PanelTransition transition))
        {
            transition = panel.AddComponent<PanelTransition>();
            transition.baseScale = panel.transform.localScale;
            if (!panel.TryGetComponent(out transition.group)) transition.group = panel.AddComponent<CanvasGroup>();
        }
        return transition;
    }

    private void PlayShow()
    {
        bool wasOff = !gameObject.activeSelf;
        bool wasHiding = isHiding;
        if (!wasOff && !wasHiding) return;   // 이미 보이는 중

        KillTween();
        isHiding = false;
        gameObject.SetActive(true);
        group.interactable = true;
        group.blocksRaycasts = true;

        // 꺼져 있었으면 처음부터, 사라지던 중이었으면 그 자리에서 다시 나타남
        if (wasOff)
        {
            group.alpha = 0f;
            transform.localScale = baseScale * ShowFromScale;
        }
        tween = DOTween.Sequence().SetUpdate(true)
            .Join(group.DOFade(1f, ShowDuration))
            .Join(transform.DOScale(baseScale, ShowDuration).SetEase(Ease.OutBack));
    }

    private void PlayHide()
    {
        if (isHiding) return;
        KillTween();
        isHiding = true;
        group.interactable = false;
        group.blocksRaycasts = false;

        tween = DOTween.Sequence().SetUpdate(true)
            .Join(group.DOFade(0f, HideDuration))
            .Join(transform.DOScale(baseScale * HideToScale, HideDuration).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                tween = null;
                gameObject.SetActive(false);   // OnDisable에서 원래 모습으로 돌려 둠
            });
    }

    // 다음에 누가 SetActive(true)로 바로 켜도 정상으로 보이게
    private void StopAndReset()
    {
        KillTween();
        isHiding = false;
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        transform.localScale = baseScale;
    }

    private void KillTween()
    {
        tween?.Kill();
        tween = null;
    }

    // 다른 코드가 바로 끄거나 부모가 꺼져도 반쯤 사라진 모습으로 남지 않게
    private void OnDisable()
    {
        StopAndReset();
    }

    private void OnDestroy()
    {
        KillTween();
    }
}
