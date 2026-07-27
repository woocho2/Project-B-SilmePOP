using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트를 사용하기 위해 필요한 네임스페이스

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("연결 설정")]
    // 인스펙터에서 FlexibleColorPicker 오브젝트를 연결합니다.
    [SerializeField] private FlexibleColorPicker fcp;

    // 색상을 실제로 변화시킬 대상 컴포넌트를 연결합니다.
    // UI 요소라면 Image를, 2D 월드 오브젝트라면 SpriteRenderer를 사용하세요.
    [SerializeField] private Image targetImage;

    private void Start()
    {
        // 씬이 시작될 때 색상 변경 함수를 실행합니다.
        ApplyColorFromHex();
    }

    /// <summary>
    /// FCP에서 헥사코드를 가져와 타겟의 색상을 변경하는 메서드
    /// </summary>
    public void ApplyColorFromHex()
    {
        // 1. FCP로부터 헥사코드 문자열(예: "#FFFFFF")을 가져옵니다.
        string hexCode = GetCurrentHexCode();

        // 헥사코드가 비어있다면 실행을 중단합니다.
        if (string.IsNullOrEmpty(hexCode)) return;

        // 2. 문자열 형태의 헥사코드를 Unity가 이해할 수 있는 Color 타입으로 변환합니다.
        // TryParseHtmlString은 변환 성공 여부를 bool로 반환하며, 성공 시 out 매개변수로 Color 값을 넘겨줍니다.
        if (ColorUtility.TryParseHtmlString(hexCode, out Color parsedColor))
        {
            // 3. 변환에 성공했다면, 타겟 이미지의 색상을 변환된 색상으로 변경합니다.
            if (targetImage != null)
            {
                targetImage.color = parsedColor;
            }
        }
        else
        {
            // 변환 실패 시 콘솔에 경고를 띄웁니다.
            Debug.LogWarning("잘못된 헥사코드입니다: " + hexCode);
        }
    }

    /// <summary>
    /// FCP의 현재 색상을 헥사코드 문자열로 반환하는 메서드
    /// </summary>
    public string GetCurrentHexCode()
    {
        if (fcp != null)
        {
            // FCP의 public color 속성을 가져옵니다.
            Color currentColor = fcp.color;

            // ColorUtility를 사용해 RGB 헥사코드 문자열(예: FFFFFF)로 변환합니다.
            string hexCode = ColorUtility.ToHtmlStringRGB(currentColor);

            // 앞에 '#'을 붙여서 온전한 헥사코드 포맷으로 반환합니다.
            return "#" + hexCode;
        }
        return string.Empty;
    }
}