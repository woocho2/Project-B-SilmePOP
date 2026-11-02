using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] GameObject m_slimeManager;
    [SerializeField] GameObject m_dragManager;

    public float MaxGameTime => GameRules.MatchDuration;
    public float CurrentTime { get; private set; }

    public MatchSettings Settings { get; private set; }
    public BoardModel Board { get; private set; }
    public GameRandom SkillRandom { get; private set; }
    public int TargetSum => Settings != null ? Settings.TargetSum : GameRules.MinTargetSum;
    public int Score { get; private set; }
    public float LastScoreTime { get; private set; }   // 경기 시작 기준 경과 시간. 동점이면 먼저 도달한 쪽이 위

    public event UnityAction<float> OnTimerUpdated;
    public event UnityAction OnGameOver;
    public event UnityAction<int> TargetSumChanged;
    public event UnityAction<int, float> LocalScoreChanged;
    public event UnityAction NoMoreMoves;

    private Coroutine co_LifeTime;
    private bool isPaused = false;
    private bool hasEnded;
    public bool IsStarting { get; private set; }
    public bool IsPaused => isPaused || IsStarting;

    private void Start()
    {
        // The UI starts the countdown after its own initialization.
        if (UIManager_Game.Instance == null || !UIManager_Game.Instance.isActiveAndEnabled)
            StartGameTimer();
    }

    public void PrepareStartCountdown()
    {
        StopGameTimer();
        CreateMatch();
        hasEnded = false;
        IsStarting = true;
        isPaused = false;
        CurrentTime = MaxGameTime;
        // Reveal the board only after the countdown text has finished.
        if (m_slimeManager != null) m_slimeManager.SetActive(false);
        if (m_dragManager != null) m_dragManager.SetActive(false);
        OnTimerUpdated?.Invoke(CurrentTime);
    }

    // 방에서 시작한 판이면 true. 게임 씬을 바로 실행해 혼자 시험할 때는 false.
    private bool IsNetworkMatch => NetworkManager.Instance != null && NetworkManager.Instance.CurrentMatch != null;

    // 카운트다운 시작까지 남은 시간. 모든 참가자가 같은 서버 시각에 시작함.
    public float SecondsUntilStart
    {
        get
        {
            if (!IsNetworkMatch) return 0f;
            return Mathf.Max(0f, (Settings.StartAt - NetworkManager.Instance.ServerNow) / 1000f);
        }
    }

    // 새 판을 만듦. 방의 값이 있으면 그것을 쓰고, 없으면 혼자 시험하는 판을 만듦.
    private void CreateMatch()
    {
        Settings = IsNetworkMatch ? NetworkManager.Instance.CurrentMatch : MatchSettings.CreateNew(0L, 1);
        Board = BoardGenerator.Generate(Settings.Seed);
        SkillRandom = new GameRandom(Settings.Seed);
        Score = 0;
        LastScoreTime = 0f;
        TargetSumChanged?.Invoke(TargetSum);
        LocalScoreChanged?.Invoke(Score, LastScoreTime);
    }

    public void StartGameTimer()
    {
        IsStarting = false;
        if (Board == null) CreateMatch();
        hasEnded = false;
        CurrentTime = MaxGameTime;
        isPaused = false;

        if (m_slimeManager != null)
        {
            m_slimeManager.SetActive(true);
            m_slimeManager.GetComponent<SlimeManager>().ShowBoard(Board);
        }
        if (m_dragManager != null) m_dragManager.SetActive(true);

        if (co_LifeTime != null)
        {
            StopCoroutine(co_LifeTime);
        }

        co_LifeTime = StartCoroutine(Co_GameTimer());
    }

    private IEnumerator Co_GameTimer()
    {
        OnTimerUpdated?.Invoke(CurrentTime);

        while (CurrentTime > 0f)
        {
            if (isPaused)
            {
                yield return null;
                continue;
            }

            yield return null;
            if (isPaused) continue;
            int previousSecond = Mathf.CeilToInt(CurrentTime);
            CurrentTime = Mathf.Max(0f, CurrentTime - Time.deltaTime);
            if (Mathf.CeilToInt(CurrentTime) != previousSecond)
                OnTimerUpdated?.Invoke(CurrentTime);
        }

        // 타이머가 끝나서 자연스럽게 게임 오버 될 때 호출
        co_LifeTime = null;
        GameOver();
    }

    public void AddScore(int scoreToAdd)
    {
        Score += scoreToAdd;
        LastScoreTime = MaxGameTime - CurrentTime;
        LocalScoreChanged?.Invoke(Score, LastScoreTime);
        if (IsNetworkMatch) NetworkManager.Instance.ReportMyScore(Score, LastScoreTime);
    }

    // 판이 바뀐 뒤 부름. 막혀도 판을 끝내지 않고 알리기만 함 (120초는 항상 끝까지).
    public void CheckForMoves()
    {
        if (!MatchFinder.HasAnyMatch(Board, TargetSum))
        {
            NoMoreMoves?.Invoke();
        }
    }

    // 개발용 치트(DebugCheat)에서만 부름.
    public void SetTargetSumForDebug(int targetSum)
    {
        Settings = new MatchSettings(Settings.Seed, targetSum, Settings.StartAt, Settings.SlotCount);
        TargetSumChanged?.Invoke(TargetSum);
    }

    public void PauseGame()
    {
        isPaused = true;
        if (m_slimeManager != null) m_slimeManager.SetActive(false);
        if (m_dragManager != null) m_dragManager.SetActive(false);
    }

    public void ResumeGame()
    {
        if (hasEnded || IsStarting) return;
        isPaused = false;
        if (m_slimeManager != null) m_slimeManager.SetActive(true);
        if (m_dragManager != null) m_dragManager.SetActive(true);
    }

    public void RestartGame()
    {
        hasEnded = false;
        Board = null;
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

        if (UIManager_Game.Instance != null && UIManager_Game.Instance.isActiveAndEnabled)
            UIManager_Game.Instance.BeginStartCountdown();
        else
            StartGameTimer();
    }

    public void GameOver()
    {
        if (hasEnded) return;
        hasEnded = true;
        StopGameTimer();
        SoundManager.Play(SoundEffect.GameOver);
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
}
