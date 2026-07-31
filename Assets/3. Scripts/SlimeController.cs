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

    public int CurrentNumber { get; private set; }

    // 콜라이더 제어용 변수 추가
    private BoxCollider2D slimeCollider;

    private void Awake()
    {
        if (highlightObject != null)
        {
            highlightRenderer = highlightObject.GetComponent<SpriteRenderer>();
        }

        // 부착된 BoxCollider2D 컴포넌트 가져오기
        slimeCollider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        SetRandomNumber();
        SetHighlight(false);
    }

    private void SetRandomNumber()
    {
        float randomValue = Random.Range(0f, 100f);

        if (randomValue < 12f) CurrentNumber = 1;
        else if (randomValue < 24f) CurrentNumber = 2;
        else if (randomValue < 36f) CurrentNumber = 3;

        else if (randomValue < 46f) CurrentNumber = 7;
        else if (randomValue < 56f) CurrentNumber = 8;
        else if (randomValue < 66f) CurrentNumber = 9;

        else
        {
            float third = 34f / 3f;

            if (randomValue < 66f + third) CurrentNumber = 4;
            else if (randomValue < 66f + (third * 2f)) CurrentNumber = 5;
            else CurrentNumber = 6;
        }

        if (numberText != null)
        {
            numberText.text = CurrentNumber.ToString();
        }
    }

    public void SetNumber(int newNumber)
    {
        CurrentNumber = newNumber;
        if (numberText != null)
        {
            numberText.text = CurrentNumber.ToString();
        }
    }

    public void SetSlimeColor(Color slimeColor)
    {
        if (numberText != null)
        {
            Color oppositeColor = GetOppositeColor(slimeColor);
            numberText.color = oppositeColor;
        }
    }

    private Color GetOppositeColor(Color originalColor)
    {
        float luminance = (originalColor.r * 0.299f) + (originalColor.g * 0.587f) + (originalColor.b * 0.114f);
        return luminance > 0.5f ? Color.black : Color.white;
    }

    public void SetHighlight(bool isActive)
    {
        if (isHintActive && !isActive)
        {
            // 힌트 유지용 주석
        }

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