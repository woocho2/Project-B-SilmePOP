using System.Collections;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

public class UIManager_Start : MonoBehaviour
{
    public static UIManager_Start Instance { get; private set; }

    [SerializeField] Image img_GameStart;
    [SerializeField] Button btn_GameStart;
    [SerializeField] string m_sceneName;

    Coroutine m_twinkleCo;

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
        if (img_GameStart != null)
        {
            m_twinkleCo = StartCoroutine(TwinkleGameStart());
        }

        if (btn_GameStart != null)
        {
            btn_GameStart.onClick.RemoveAllListeners();
            btn_GameStart.onClick.AddListener(() =>
            {
                SceneLoader.StartLoad(m_sceneName);
            });
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    IEnumerator TwinkleGameStart()
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

            img_GameStart.enabled = false;
        }
    }
}
