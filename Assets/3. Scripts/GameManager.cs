using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float maxGameTime = 120f;
    [SerializeField] GameObject m_slimeManager;
    [SerializeField] GameObject m_dragManager;

    public float MaxGameTime => maxGameTime;
    public float CurrentTime { get; private set; }

    public event UnityAction<float> OnTimerUpdated;
    public event UnityAction OnGameOver;

    private Coroutine co_LifeTime;
    private Coroutine co_delayedGameOver;
    private bool isPaused = false;

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
        StartGameTimer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartGameTimer()
    {
        CurrentTime = maxGameTime;
        isPaused = false;

        if (co_LifeTime != null)
        {
            StopCoroutine(co_LifeTime);
        }

        co_LifeTime = StartCoroutine(Co_GameTimer());
    }

    private IEnumerator Co_GameTimer()
    {
        WaitForSeconds oneSecond = new WaitForSeconds(1f);

        OnTimerUpdated?.Invoke(CurrentTime);

        while (CurrentTime > 0f)
        {
            if (isPaused)
            {
                yield return null;
                continue;
            }

            yield return oneSecond;

            CurrentTime -= 1f;

            if (CurrentTime < 0f)
            {
                CurrentTime = 0f;
            }

            OnTimerUpdated?.Invoke(CurrentTime);
        }

        // 타이머가 끝나서 자연스럽게 게임 오버 될 때 호출
        GameOver();
    }


    public void PauseGame()
    {
        isPaused = true;
        m_slimeManager.SetActive(false);
        m_dragManager.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        m_slimeManager.SetActive(true);
        m_dragManager.SetActive(true);
    }

    public void RestartGame()
    {
        ResumeGame();

        if (m_slimeManager != null)
        {
            SlimeManager slimemanager = m_slimeManager.GetComponent<SlimeManager>();
            if (slimemanager != null)
            {
                slimemanager.ResetSlimeManager();
            }
        }

        if (m_dragManager != null)
        {
            DragManager dragmanager = m_dragManager.GetComponent<DragManager>();
            if (dragmanager != null)
            {
                dragmanager.ResetDragManager();
            }
        }

        StartGameTimer();
    }

    public void CheckAndTriggerGameOver()
    {
        if (SlimeManager.Instance != null && !SlimeManager.Instance.HasAvailableMatches())
        {
            Debug.Log("더 이상 맞출 수 있는 슬라임이 없습니다.");
              
            bool canUseSkill = false;

            if (UIManager_Game.Instance != null)
            {
                canUseSkill = UIManager_Game.Instance.HasAnySkillLeft();
            }

            if (canUseSkill)
            {
                if (UIManager_Game.Instance != null)
                {
                    UIManager_Game.Instance.ShowWarningText("더 이상 맞출 수 있는 슬라임이 없습니다.\n스킬을 사용하세요");
                    UIManager_Game.Instance.HighlightAvailableSkills();
                }
            }
            else if (co_delayedGameOver == null)
            {
                co_delayedGameOver = StartCoroutine(Co_DelayedGameOver(3.5f));
            }
        }
    }

    public void GameOver()
    {
        StopGameTimer();
        OnGameOver?.Invoke();
        PauseGame();
        Debug.Log("게임 종료");
    }


    public void StopGameTimer()
    {
        if (co_LifeTime != null)
        {
            StopCoroutine(co_LifeTime);
            co_LifeTime = null;
        }
    }

    private IEnumerator Co_DelayedGameOver(float delayTime)
    {
        StopGameTimer();

        if (UIManager_Game.Instance != null)
        {
            UIManager_Game.Instance.ShowWarningText("더 이상 맞출 수 있는 슬라임이 없습니다!");
        }

        yield return new WaitForSeconds(delayTime);

        co_delayedGameOver = null;
        GameOver();
    }
}