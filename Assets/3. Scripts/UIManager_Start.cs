using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager_Start : MonoBehaviour
{
    public static UIManager_Start Instance { get; private set; }

    [Header("Logo Settings")]
    [SerializeField] private RectTransform rect_Logo; // 로고 크기 애니메이션을 위한 RectTransform
    [SerializeField] private Image img_Logo;          // 로고 이미지를 표시할 Image 컴포넌트
    
    [Header("Start Button Settings")]
    [SerializeField] private RectTransform rect_GameStart; // 버튼 크기 애니메이션을 위한 RectTransform
    [SerializeField] private Image img_GameStart;
    [SerializeField] private Button btn_GameStart;

    [Header("Scene Settings")]
    [SerializeField] private string m_sceneName;

    private Coroutine m_twinkleCo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    { 
        // 2. 버튼 클릭 이벤트 세팅 및 연출 중 클릭 방지
        if (btn_GameStart != null)
        {
            btn_GameStart.onClick.RemoveAllListeners();
            btn_GameStart.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
            btn_GameStart.onClick.AddListener(() =>
            {
                SceneLoader.StartLoad(m_sceneName);
            });

            // 연출이 끝나기 전까지 유저가 누를 수 없도록 버튼 비활성화
            btn_GameStart.interactable = false;
        }

        // 3. 인트로 연출 시작
        StartCoroutine(IntroSequence());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 게임 시작 시 순차적인 UI 연출을 담당하는 코루틴입니다.
    /// </summary>
    private IEnumerator IntroSequence()
    {
        // 1. 초기 상태 세팅: 두 오브젝트를 보이지 않게(Scale 0) 처리
        if (rect_Logo != null) rect_Logo.localScale = Vector3.zero;
        if (rect_GameStart != null) rect_GameStart.localScale = Vector3.zero;

        // 2. 로고가 화면에 꽂히는 연출 실행 및 대기
        if (rect_Logo != null)
        {
            yield return StartCoroutine(SlamAnimation(rect_Logo));
        }

        // 3. 정확히 1초 대기
        yield return new WaitForSeconds(1f);

        // 4. 게임 시작 버튼이 화면에 꽂히는 연출 실행 및 대기
        if (rect_GameStart != null)
        {
            yield return StartCoroutine(SlamAnimation(rect_GameStart));
        }

        // 5. 연출이 모두 끝나면 버튼 클릭을 허용하고 기존의 반짝임 효과 시작
        if (btn_GameStart != null)
        {
            btn_GameStart.interactable = true;
        }

        if (img_GameStart != null)
        {
            m_twinkleCo = StartCoroutine(TwinkleGameStart());
        }
    }

    /// <summary>
    /// 타겟 UI가 플레이어 쪽(Scale 5)에서 화면(Scale 1)으로 강하게 꽂히는 애니메이션을 재생합니다.
    /// </summary>
    private IEnumerator SlamAnimation(RectTransform target)
    {
        float duration = 0.25f; // 꽂히는 데 걸리는 시간 (빠를수록 타격감이 좋습니다)
        float elapsed = 0f;

        // 시작 스케일(아주 크게)과 목표 스케일(원래 크기 1) 설정
        Vector3 startScale = new Vector3(5f, 5f, 5f);
        Vector3 endScale = new Vector3(0.75f,0.75f,0.75f);

        target.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Ease-In 효과: 처음엔 빠르게 다가오고 마지막에 확 멈추는 수학적 연출
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            target.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        target.localScale = endScale; // 오차 보정을 위해 정확히 1로 고정
    }

    /// <summary>
    /// 게임 시작 버튼이 서서히 반짝거리는 기존 코루틴입니다.
    /// </summary>
    private IEnumerator TwinkleGameStart()
    {
        float speed = 0.8f;
        float minAlpha = 0.2f;
        float maxAlpha = 1.0f;

        while (true)
        {
            float pingpongValue = Mathf.PingPong(Time.time * speed, 1f);
            float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, pingpongValue);

            Color currentColor = img_GameStart.color;
            currentColor.a = currentAlpha;
            img_GameStart.color = currentColor;

            yield return null;
        }
    }

    public void StopTwinkle()
    {
        if (m_twinkleCo != null)
        {
            StopCoroutine(m_twinkleCo);
            m_twinkleCo = null;

            if (img_GameStart != null)
            {
                img_GameStart.enabled = false;
            }
        }
    }
}