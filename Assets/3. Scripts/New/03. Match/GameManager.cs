using System.Collections;
using System.Collections.Generic;
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
    public int TargetSum => Settings != null ? Settings.TargetSum : GameRules.MinTargetSum;
    public int Score { get; private set; }
    public float LastScoreTime { get; private set; }   // 경기 시작 기준 경과 시간. 동점이면 먼저 도달한 쪽이 위

    public event UnityAction<float> OnTimerUpdated;
    public event UnityAction OnGameOver;
    public event UnityAction<int> TargetSumChanged;
    public event UnityAction<int, float> LocalScoreChanged;
    public event UnityAction NoMoreMoves;
    public event UnityAction MatchCreated;   // 새 판(Settings, Board)이 만들어짐. 스킬 횟수 등을 처음부터

    private Coroutine co_LifeTime;
    private readonly List<AiPlayer> aiPlayers = new List<AiPlayer>();
    private bool hasEnded;
    public bool IsStarting { get; private set; }

    // 120초가 흐르는 중. 카운트다운 중과 끝난 뒤에는 입력을 받지 않음.
    // 한번 시작한 판은 기권 말고는 멈출 수 없음 (일시정지 없음).
    public bool IsPlaying => !IsStarting && !hasEnded && co_LifeTime != null;

    private void Start()
    {
        // 씬에 SkillManager를 놓지 않았어도 단축키(1 ~ 6)로 스킬을 쓸 수 있게 만들어 둠 (버튼은 씬에 놓고 연결해야 보임)
        if (SkillManager.Instance == null) new GameObject(nameof(SkillManager)).AddComponent<SkillManager>();

        // The UI starts the countdown after its own initialization.
        if (UIManager_Game.Instance == null || !UIManager_Game.Instance.isActiveAndEnabled)
            StartGameTimer();
    }

    // 조커는 5초마다 빈자리로 이동 (판이 진행 중일 때만). AI 판은 AiPlayer가 따로 돌림.
    private void Update()
    {
        if (IsPlaying && Board != null) Board.Tick(Time.deltaTime);
    }

    public void PrepareStartCountdown()
    {
        StopGameTimer();
        ClearAiPlayers();
        CreateMatch();
        hasEnded = false;
        IsStarting = true;
        CurrentTime = MaxGameTime;
        // Reveal the board only after the countdown text has finished.
        if (m_slimeManager != null) m_slimeManager.SetActive(false);
        if (m_dragManager != null) m_dragManager.SetActive(false);
        OnTimerUpdated?.Invoke(CurrentTime);

        // 게임 씬에 도착했음을 알림. 모두 오면 playAt이 정해져 카운트다운이 시작됨.
        if (IsNetworkMatch) NetworkManager.Instance.ReportLoaded();
    }

    // 방에서 시작한 판이면 true. 게임 씬을 바로 실행해 혼자 시험할 때는 false.
    private bool IsNetworkMatch => NetworkManager.Instance != null && NetworkManager.Instance.CurrentMatch != null;

    // 모두 게임 씬에 모여 3, 2, 1을 시작할 시각(playAt)이 정해졌는가. 혼자 시험하는 판은 바로 시작.
    public bool IsReadyToCountdown => !IsNetworkMatch || PlayAt > 0;

    // 3, 2, 1 시작까지 남은 시간. 모든 참가자가 같은 서버 시각에 시작함.
    public float SecondsUntilStart
    {
        get
        {
            if (!IsNetworkMatch || PlayAt <= 0) return 0f;
            return Mathf.Max(0f, (PlayAt - NetworkManager.Instance.ServerNow) / 1000f);
        }
    }

    private long PlayAt => NetworkManager.Instance.CurrentRoom != null ? NetworkManager.Instance.CurrentRoom.playAt : 0;

    // 새 판을 만듦. 방의 값이 있으면 그것을 쓰고, 없으면 혼자 시험하는 판을 만듦.
    private void CreateMatch()
    {
        Settings = IsNetworkMatch ? NetworkManager.Instance.CurrentMatch : MatchSettings.CreateNew(0L, 1);
        Board = BoardGenerator.Generate(Settings.Seed);
        Score = 0;
        LastScoreTime = 0f;
        TargetSumChanged?.Invoke(TargetSum);
        LocalScoreChanged?.Invoke(Score, LastScoreTime);
        MatchCreated?.Invoke();
    }

    // 방장만 AI 자리를 맡음 (다른 PC에서도 만들면 점수가 두 번 올라감). 혼자 시험하는 판에는 AI가 없음.
    private void SpawnAiPlayers()
    {
        ClearAiPlayers();
        if (!IsNetworkMatch || !NetworkManager.Instance.IsHost) return;

        RoomInfo room = NetworkManager.Instance.CurrentRoom;
        if (room == null) return;

        for (int slot = 0; slot < room.slots.Length; slot++)
        {
            if (room.slots[slot].kind == SlotKind.Ai)
            {
                aiPlayers.Add(AiPlayer.Create(slot, room.slots[slot].difficulty, Settings));
            }
        }
    }

    private void ClearAiPlayers()
    {
        foreach (AiPlayer ai in aiPlayers)
        {
            if (ai != null) Destroy(ai.gameObject);
        }
        aiPlayers.Clear();
    }

    public void StartGameTimer()
    {
        IsStarting = false;
        if (Board == null) CreateMatch();
        hasEnded = false;
        CurrentTime = MaxGameTime;

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
        SpawnAiPlayers();
    }

    private IEnumerator Co_GameTimer()
    {
        OnTimerUpdated?.Invoke(CurrentTime);

        while (CurrentTime > 0f)
        {
            yield return null;
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

    // 게임오버 화면의 다시 하기. 카운트다운부터 새 판으로 시작함.
    public void RestartGame()
    {
        hasEnded = false;
        Board = null;

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
        if (m_slimeManager != null) m_slimeManager.SetActive(false);
        if (m_dragManager != null) m_dragManager.SetActive(false);
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
