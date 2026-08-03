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
        /*// 1. 기준 확률 계산
        float pMid = 100f / 9f;             // 중간값 확률 (약 11.11%)
        float pLow = pMid + 0.25f;          // 낮은값 확률 (약 11.36%)
        float pHigh = pMid - 0.25f;         // 높은값 확률 (약 10.86%)

        // 2. 누적 구간 계산
        float threshold1 = pLow;
        float threshold2 = threshold1 + pLow;
        float threshold3 = threshold2 + pLow;

        float threshold4 = threshold3 + pMid;
        float threshold5 = threshold4 + pMid;
        float threshold6 = threshold5 + pMid;

        float threshold7 = threshold6 + pHigh;
        float threshold8 = threshold7 + pHigh;

        float randomValue = Random.Range(0f, 100f);

        // 3. 번호 할당
        if (randomValue < threshold1) CurrentNumber = 1;
        else if (randomValue < threshold2) CurrentNumber = 2;
        else if (randomValue < threshold3) CurrentNumber = 3;

        else if (randomValue < threshold4) CurrentNumber = 4;
        else if (randomValue < threshold5) CurrentNumber = 5;
        else if (randomValue < threshold6) CurrentNumber = 6;

        else if (randomValue < threshold7) CurrentNumber = 7;
        else if (randomValue < threshold8) CurrentNumber = 8;
        else CurrentNumber = 9;*/

        CurrentNumber = 9;

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