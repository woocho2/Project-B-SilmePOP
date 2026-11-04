using UnityEngine;

// 게임 씬. 패널을 켜고 끄며 판의 단계만 넘김. 각 패널의 일은 패널에 붙은 스크립트가 함.
//   Panel_Ready    (Game_ReadyPanel) 모두 모이면 목표 점수·아이템 3개 뽑기
//   Panel_Game     (Game_GamePanel)  3, 2, 1 카운트다운, 시간, 점수, 막혔을 때 안내
//   Panel_Rank     (Game_RankPanel)  실시간 순위
//   Panel_GameOver                   판이 끝나면 (선택 사항, 결과 화면은 나중에)
// 순서: 뽑기 → 뽑기 화면 닫고 3, 2, 1 → GameManager.StartGameTimer. 아이템 칸은 SkillManager.
public class UIManager_Game : Singleton<UIManager_Game>
{
    [Header("Panels")]
    [Tooltip("비우면 Game_ReadyPanel이 붙은 오브젝트를 찾음. 없으면 뽑기 없이 바로 3, 2, 1")]
    [SerializeField] private GameObject Panel_Ready;
    [Tooltip("Game_GamePanel이 붙은 오브젝트")]
    [SerializeField] private GameObject Panel_Game;
    [Tooltip("비우면 Game_RankPanel이 붙은 오브젝트를 찾음")]
    [SerializeField] private GameObject Panel_Rank;
    [SerializeField] private GameObject Panel_GameOver;

    private Game_ReadyPanel readyPanel;
    private Game_GamePanel gamePanel;

    private void Start()
    {
        if (Instance != this) return;

        if (Panel_Ready == null) Panel_Ready = FindPanel<Game_ReadyPanel>();
        if (Panel_Rank == null) Panel_Rank = FindPanel<Game_RankPanel>();
        if (Panel_Game == null) Panel_Game = FindPanel<Game_GamePanel>();
        readyPanel = Panel_Ready != null ? Panel_Ready.GetComponent<Game_ReadyPanel>() : null;
        gamePanel = Panel_Game != null ? Panel_Game.GetComponent<Game_GamePanel>() : null;

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

    private void ShowGameOver()
    {
        if (Panel_GameOver != null) PanelTransition.Show(Panel_GameOver);
    }

    private static GameObject FindPanel<T>() where T : Component
    {
        T panel = FindAnyObjectByType<T>(FindObjectsInactive.Include);
        return panel != null ? panel.gameObject : null;
    }
}
