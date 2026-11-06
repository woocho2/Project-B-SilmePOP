using UnityEngine;

// 게임 씬. 패널을 켜고 끄며 판의 단계만 넘김. 각 패널의 일은 패널에 붙은 스크립트가 함.
//   Panel_Ready    (Game_ReadyPanel) 모두 모이면 목표 점수·아이템 3개 뽑기
//   Panel_Game     (Game_GamePanel)  3, 2, 1 카운트다운, 시간, 점수, 막혔을 때 안내
//   Panel_Rank     (Game_RankPanel)  실시간 순위
//   Panel_GameOver (Game_EndPanel)   판이 끝나면: 다른 패널을 다 끄고 1 ~ 4등과 최종 판, 룸으로 가기 (20초 뒤 자동)
// 순서: 뽑기 → 뽑기 화면 닫고 3, 2, 1 → GameManager.StartGameTimer → 시간 끝 → 결과 → 룸 씬.
// 아이템 칸은 SkillManager.
public class UIManager_Game : Singleton<UIManager_Game>
{
    private const string EndPanelName = "GameEndPanel";

    [Header("Panels")]
    [Tooltip("비우면 Game_ReadyPanel이 붙은 오브젝트를 찾음. 없으면 뽑기 없이 바로 3, 2, 1")]
    [SerializeField] private GameObject Panel_Ready;
    [Tooltip("Game_GamePanel이 붙은 오브젝트")]
    [SerializeField] private GameObject Panel_Game;
    [Tooltip("비우면 Game_RankPanel이 붙은 오브젝트를 찾음")]
    [SerializeField] private GameObject Panel_Rank;
    [Tooltip("결과 화면. 비우면 Game_EndPanel이 붙은 오브젝트, 없으면 이름 GameEndPanel을 찾아 Game_EndPanel을 붙임")]
    [SerializeField] private GameObject Panel_GameOver;

    [Header("Scene Settings")]
    [Tooltip("결과 화면에서 룸으로 가기 (방에서 시작한 판일 때)")]
    [SerializeField] private string m_roomSceneName = "1. Scenes/3. Room";

    private Game_ReadyPanel readyPanel;
    private Game_GamePanel gamePanel;
    private Game_EndPanel endPanel;
    private bool isLeavingScene;

    private void Start()
    {
        if (Instance != this) return;

        if (Panel_Ready == null) Panel_Ready = FindPanel<Game_ReadyPanel>();
        if (Panel_Rank == null) Panel_Rank = FindPanel<Game_RankPanel>();
        if (Panel_Game == null) Panel_Game = FindPanel<Game_GamePanel>();
        if (Panel_GameOver == null) Panel_GameOver = FindPanel<Game_EndPanel>() ?? FindByName(EndPanelName);
        readyPanel = Panel_Ready != null ? Panel_Ready.GetComponent<Game_ReadyPanel>() : null;
        gamePanel = Panel_Game != null ? Panel_Game.GetComponent<Game_GamePanel>() : null;
        if (Panel_GameOver != null)
        {
            endPanel = Panel_GameOver.GetComponent<Game_EndPanel>();
            if (endPanel == null) endPanel = Panel_GameOver.AddComponent<Game_EndPanel>();
        }

        if (GameManager.Instance != null) GameManager.Instance.OnGameOver += ShowGameOver;
        BeginMatch();
    }

    protected override void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameOver -= ShowGameOver;
        base.OnDestroy();
    }

    // 새 판: 게임·순위 화면 위에 뽑기 화면을 띄우고 뽑기부터
    public void BeginMatch()
    {
        GameManager game = GameManager.Instance;
        if (game == null) return;

        if (readyPanel != null) readyPanel.Stop();
        if (gamePanel != null) gamePanel.StopCountdown();
        game.PrepareStartCountdown();

        PanelTransition.SetImmediate(Panel_GameOver, false);
        PanelTransition.SetImmediate(Panel_Game, true);
        PanelTransition.SetImmediate(Panel_Rank, true);

        if (readyPanel == null)
        {
            PlayCountdown();
            return;
        }
        PanelTransition.SetImmediate(Panel_Ready, true);
        readyPanel.Begin(OnReadyFinished);
    }

    // 뽑기 끝: 목표 점수 슬라임을 게임 화면의 시간 글자 위로 보내고, 뽑기 화면을 닫고 3, 2, 1
    private void OnReadyFinished()
    {
        if (gamePanel != null) readyPanel.SendTargetSlime(gamePanel.TimerRect);
        PanelTransition.Hide(Panel_Ready);
        PlayCountdown();
    }

    private void PlayCountdown()
    {
        if (gamePanel != null) gamePanel.PlayCountdown(StartGame);
        else StartGame();
    }

    private void StartGame()
    {
        if (GameManager.Instance != null) GameManager.Instance.StartGameTimer();
    }

    // 시간이 끝남 (AI·입력은 GameManager.GameOver가 이미 멈춤): 마지막 판 위에 TIME UP!을 찍은 뒤 결과 화면
    private void ShowGameOver()
    {
        if (readyPanel != null) readyPanel.Stop();
        if (gamePanel != null && Panel_Game != null && Panel_Game.activeInHierarchy) gamePanel.PlayTimeUp(ShowEndPanel);
        else ShowEndPanel();
    }

    // 다른 패널과 판을 다 끄고 결과 화면만
    private void ShowEndPanel()
    {
        if (gamePanel != null) gamePanel.StopCountdown();
        if (GameManager.Instance != null) GameManager.Instance.HideBoard();
        PanelTransition.SetImmediate(Panel_Ready, false);
        PanelTransition.SetImmediate(Panel_Game, false);
        PanelTransition.SetImmediate(Panel_Rank, false);

        if (Panel_GameOver == null)
        {
            Debug.LogWarning("UIManager_Game: 결과 화면(GameEndPanel)이 없어 바로 룸으로 갑니다.", this);
            ReturnToRoom();
            return;
        }
        PanelTransition.Show(Panel_GameOver);
        if (endPanel != null) endPanel.Show(ReturnToRoom);
    }

    // 방에서 시작한 판이면 룸 씬으로. 먼저 돌아가는 사람이 방을 대기 상태로 돌림 (다시 시작할 수 있게).
    // 게임 씬을 혼자 시험하는 중이면 새 판.
    private void ReturnToRoom()
    {
        // 먼저 돌아간 사람 때문에 판 정보(CurrentMatch)가 이미 지워졌을 수 있으므로 방이 있는지로 가름
        NetworkManager network = NetworkManager.Instance;
        if (network == null || network.CurrentRoom == null)
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            return;
        }

        if (isLeavingScene) return;
        isLeavingScene = true;
        // 결과 단계일 때만 (그사이 방장이 새 판을 시작했으면 건드리지 않음)
        if (network.CurrentRoom != null && network.CurrentRoom.state == RoomState.Result) network.ReturnToRoom();
        SceneLoader.StartLoad(m_roomSceneName);
    }

    private static GameObject FindPanel<T>() where T : Component
    {
        T panel = FindAnyObjectByType<T>(FindObjectsInactive.Include);
        return panel != null ? panel.gameObject : null;
    }

    private static GameObject FindByName(string objectName)
    {
        foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rect.name == objectName && rect.gameObject.scene.IsValid()) return rect.gameObject;
        }
        return null;
    }
}
