using TMPro;
using UnityEngine;

public class SlimeController : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private TextMeshProUGUI numberText;

    [Header("Highlight Component")]
    [SerializeField] private GameObject highlightObject;
    private SpriteRenderer highlightRenderer;
    private readonly Color hintColor = new Color(1f, 1f, 0f, 0.8f); // 노란색 (힌트용)
    private bool isHintActive = false; // 현재 힌트 상태인지 추적
    public int CurrentNumber { get; private set; }

    private void Awake()
    {
        if (highlightObject != null)
        {
            highlightRenderer = highlightObject.GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        SetRandomNumber();
        SetHighlight(false);
    }

    private void SetRandomNumber()
    {
        // 0.0f ~ 100.0f 사이의 부동소수점 난수 생성
        float randomValue = Random.Range(0f, 100f);

        // 누적 확률에 따라 조건 판별
        if (randomValue < 12f) CurrentNumber = 1;              // 0 ~ 12% 구간 (12%)
        else if (randomValue < 24f) CurrentNumber = 2;         // 12 ~ 24% 구간 (12%)
        else if (randomValue < 36f) CurrentNumber = 3;         // 24 ~ 36% 구간 (12%)

        else if (randomValue < 46f) CurrentNumber = 7;         // 36 ~ 46% 구간 (10%)
        else if (randomValue < 56f) CurrentNumber = 8;         // 46 ~ 56% 구간 (10%)
        else if (randomValue < 66f) CurrentNumber = 9;         // 56 ~ 66% 구간 (10%)

        else
        {
            // 나머지 34% 구간(66 ~ 100)을 3등분하여 4, 5, 6에 할당 (각 약 11.33%)
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

    /// <summary>
    /// 외부에서 슬라임의 번호를 명시적으로 변경할 때 사용하는 메서드입니다.
    /// (Mix 기능에서 사용됨)
    /// </summary>
    public void SetNumber(int newNumber)
    {
        CurrentNumber = newNumber;
        if (numberText != null)
        {
            numberText.text = CurrentNumber.ToString();
        }
    }

    // 슬라임 색상 변경 시 외부(SlimeAppearance 등)에서 호출할 메서드
    public void SetSlimeColor(Color slimeColor)
    {
        if (numberText != null)
        {
            Color oppositeColor = GetOppositeColor(slimeColor);
            numberText.color = oppositeColor;
        }
    }

    // RGB 반전으로 보색 계산
    private Color GetOppositeColor(Color originalColor)
    {
        // 원본 색상의 명암을 먼저 계산
        float luminance = (originalColor.r * 0.299f) + (originalColor.g * 0.587f) + (originalColor.b * 0.114f);

        // 원본이 밝은 색(0.5 이상)이면 검은색 반환, 어두운 색이면 흰색 반환
        return luminance > 0.5f ? Color.black : Color.white;
    }

    public void SetHighlight(bool isActive)
    {
        // 힌트가 켜져있는 상태라면 플레이어의 드래그 하이라이트가 힌트를 덮어씌워야 함
        if (isHintActive && !isActive)
        {
            // 드래그가 끝났을 때 힌트 상태를 유지하려면 끄지 않음
            // 만약 드래그가 지나가면 힌트를 지우고 싶다면 isHintActive = false; 를 추가하세요.
        }

        if (highlightObject != null)
        {
            highlightObject.SetActive(isActive || isHintActive);
        }
    }

    public void SetHintHighlight(bool isActive)
    {
        isHintActive = isActive;

        if (highlightObject != null && highlightRenderer != null)
        {
            highlightObject.SetActive(isActive);
            if (isActive)
            {
                highlightRenderer.color = hintColor; // 노란색 칠하기
            }
        }
    }

    public void SetHighlightColor(Color color)
    {
        if (highlightRenderer != null)
        {
            highlightRenderer.color = color;

            // 플레이어가 드래그로 색을 바꿨다면 힌트 상태는 해제된 것으로 간주
            isHintActive = false;
        }
    }
}