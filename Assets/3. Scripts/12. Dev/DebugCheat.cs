#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

// 개발용 키. 에디터와 개발 빌드에서만 있고 출시 빌드에는 들어가지 않음. 씬에 놓지 않아도 저절로 생김.
//   F1: 목표 합을 10 → 11 → ... → 15 → 10 순서로 바꿈 (기록 중에는 안 됨)
//   T: 타이틀 화면의 글자 등장 연출을 처음부터 다시 재생
//      게임 화면(혼자 하는 판)에서는 기록 모드: 목표 합(10 ~ 15)을 적고 Enter → 그 목표 합으로 10판 연속.
//      판마다 행동 기록을 TestRecords/player_games.csv에, 10판이 끝나면 요약을 player_sessions.csv에 남기고 분석을 보여 줌.
//      묻는 중에는 T·Esc로 취소, 기록 중에는 T를 두 번 눌러 중단, 분석 화면은 T로 닫음.
public class DebugCheat : MonoBehaviour
{
    private const int GamesPerSession = 10;
    private const float NextGameDelay = 3f;      // 판이 끝나고 다음 판까지 (결과를 볼 시간)
    private const float StartMessageTime = 3f;
    private const float AbortConfirmTime = 2f;   // 기록 중 T를 한 번 더 누르면 중단

    private enum Phase { Off, Asking, Recording, Done }

    private Phase phase;
    private string typed = "";
    private string notice;            // 잠깐 보여 줄 안내
    private float noticeUntil;
    private float abortArmedUntil;

    private GameManager game;         // 기록 중인 판. 씬이 바뀌면 사라져 기록도 끝남
    private int target;
    private string sessionId;
    private readonly List<PlayerGameLog> logs = new List<PlayerGameLog>();
    private PlayerGameLog current;    // 지금 판. 판이 끝나면 null (다음 판이 아직 안 만들어짐)
    private string lastResult;
    private string report;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject cheat = new GameObject(nameof(DebugCheat));
        cheat.AddComponent<DebugCheat>();
        DontDestroyOnLoad(cheat);
    }

    private static string RecordFolder => Application.isEditor
        ? Path.Combine(Application.dataPath, "..", "TestRecords")
        : Path.Combine(Application.persistentDataPath, "TestRecords");

    private void Update()
    {
        if (phase == Phase.Recording && game == null) StopSession("씬이 바뀌어 기록을 멈췄습니다.");
        if ((phase == Phase.Asking || phase == Phase.Done) && GameManager.Instance == null) phase = Phase.Off;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || ChatPanel.IsTyping) return;

        if (keyboard.tKey.wasPressedThisFrame)
        {
            Login_TitlePanel title = FindAnyObjectByType<Login_TitlePanel>();   // 켜져 있는 타이틀만 찾음
            if (title != null) title.ReplayIntro();
        }

        GameManager instance = GameManager.Instance;
        if (instance == null || instance.Board == null) return;

        if (keyboard.f1Key.wasPressedThisFrame && phase == Phase.Off)
        {
            int next = instance.TargetSum + 1;
            instance.SetTargetSumForDebug(next > GameRules.MaxTargetSum ? GameRules.MinTargetSum : next);
        }

        switch (phase)
        {
            case Phase.Off:
                if (keyboard.tKey.wasPressedThisFrame) Ask(instance);
                break;
            case Phase.Asking:
                UpdateAsking(keyboard, instance);
                break;
            case Phase.Recording:
                if (keyboard.tKey.wasPressedThisFrame)
                {
                    if (Time.unscaledTime < abortArmedUntil) StopSession($"기록 중단. 끝난 {logs.Count}판은 player_games.csv에 남았고 요약은 남기지 않았습니다.");
                    else
                    {
                        abortArmedUntil = Time.unscaledTime + AbortConfirmTime;
                        Notify("T를 한 번 더 누르면 기록을 중단합니다.", AbortConfirmTime);
                    }
                }
                break;
            case Phase.Done:
                if (keyboard.tKey.wasPressedThisFrame) phase = Phase.Off;
                break;
        }
    }

    // ── 목표 합 묻기 ──────────────────────────────────────────

    // 방에서 시작한 판에서는 켜지 않음 (내 점수가 서버에 올라가므로)
    private void Ask(GameManager instance)
    {
        if (instance.IsNetworkMatch)
        {
            Notify("방에서 시작한 판에서는 기록 모드를 쓸 수 없습니다.", StartMessageTime);
            return;
        }
        phase = Phase.Asking;
        typed = "";
    }

    private void UpdateAsking(Keyboard keyboard, GameManager instance)
    {
        if (keyboard.tKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
        {
            phase = Phase.Off;
            return;
        }

        for (int digit = 0; digit <= 9; digit++)
        {
            Key top = digit == 0 ? Key.Digit0 : Key.Digit1 + (digit - 1);   // 윗줄 숫자는 1 ~ 9 다음에 0
            if ((keyboard[top].wasPressedThisFrame || keyboard[Key.Numpad0 + digit].wasPressedThisFrame) && typed.Length < 2)
                typed += digit;
        }
        if (keyboard.backspaceKey.wasPressedThisFrame && typed.Length > 0) typed = typed.Substring(0, typed.Length - 1);

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            if (int.TryParse(typed, out int value) && value >= GameRules.MinTargetSum && value <= GameRules.MaxTargetSum)
            {
                StartSession(instance, value);
            }
            else
            {
                Notify($"{GameRules.MinTargetSum} ~ {GameRules.MaxTargetSum} 사이로 적어 주세요.", 2f);
                typed = "";
            }
        }
    }

    // ── 10판 기록 ─────────────────────────────────────────────

    private void StartSession(GameManager instance, int targetSum)
    {
        phase = Phase.Recording;
        game = instance;
        target = targetSum;
        sessionId = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        logs.Clear();
        current = null;
        lastResult = null;
        report = null;

        game.DebugTargetSum = targetSum;
        game.MatchCreated += OnMatchCreated;
        game.OnGameOver += OnGameOver;
        DragManager.Popped += OnPopped;
        DragManager.Missed += OnMissed;

        Notify($"{targetSum}점 {GamesPerSession}판 기록을 시작하겠습니다.", StartMessageTime);
        Debug.Log($"[기록 모드] 목표 {targetSum} · {GamesPerSession}판 시작");
        game.RestartGame();
    }

    private void StopSession(string message)
    {
        if (game != null)
        {
            game.DebugTargetSum = null;
            game.MatchCreated -= OnMatchCreated;
            game.OnGameOver -= OnGameOver;
        }
        DragManager.Popped -= OnPopped;
        DragManager.Missed -= OnMissed;
        game = null;
        current = null;
        phase = report != null ? Phase.Done : Phase.Off;
        if (message != null)
        {
            Notify(message, StartMessageTime);
            Debug.Log("[기록 모드] " + message);
        }
    }

    private void OnMatchCreated()
    {
        current = new PlayerGameLog
        {
            TargetSum = game.TargetSum,
            Seed = game.Settings.Seed,
            Item = ItemNames(game.Settings.Skills)
        };
    }

    private float Elapsed => game.MaxGameTime - game.CurrentTime;

    private void OnPopped(IReadOnlyList<BoardSlime> slimes)
    {
        if (current != null && game != null && game.IsPlaying) current.AddPop(Elapsed, slimes);
    }

    private void OnMissed(IReadOnlyList<BoardSlime> slimes)
    {
        if (current != null && game != null && game.IsPlaying) current.Misses++;
    }

    private void OnGameOver()
    {
        if (current == null) return;

        PlayerGameLog log = current;
        current = null;
        log.Removed = AiSimulator.CountRemoved(game.Board);
        log.ItemsUsed = ItemsUsed(SkillManager.Instance != null ? SkillManager.Instance.Skills : null);
        logs.Add(log);
        AppendLine("player_games.csv", PlayerGameLog.CsvHeader,
                   log.ToCsv(Now(), sessionId, logs.Count, game.MaxGameTime));

        lastResult = $"{logs.Count}/{GamesPerSession}판: {log.Removed}마리 (평균 {log.AverageGap:0.0}초에 한 번, 실수 {log.Misses})";
        Debug.Log($"[기록 모드] {lastResult}");

        if (logs.Count < GamesPerSession)
        {
            Notify($"{lastResult}. {NextGameDelay:0}초 뒤 다음 판", NextGameDelay);
            GameManager waiting = game;
            DOVirtual.DelayedCall(NextGameDelay, () =>
            {
                // 그사이 기록을 멈췄거나 다시 하기를 직접 눌러 새 판이 시작됐으면 그대로 둠
                if (phase == Phase.Recording && game == waiting && current == null) game.RestartGame();
            });
            return;
        }

        FinishSession();
    }

    private void FinishSession()
    {
        List<KeyValuePair<int, double>> history = ReadHistory();
        PlayerSessionReport summary = new PlayerSessionReport(target, new List<PlayerGameLog>(logs), game.MaxGameTime);
        report = summary.Describe(history);
        AppendLine("player_sessions.csv", PlayerSessionReport.CsvHeader, summary.ToCsv(Now()));
        Debug.Log("[기록 모드] 분석\n" + report);
        StopSession(null);
    }

    private static int ItemsUsed(SkillSet skills)
    {
        if (skills == null) return 0;
        int used = 0;
        foreach (SkillType type in skills.Available) used += skills.Get(type).MaxUses - skills.UsesLeft(type);
        return used;
    }

    // ── 파일 ──────────────────────────────────────────────────

    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static void AppendLine(string file, string header, string line)
    {
        try
        {
            Directory.CreateDirectory(RecordFolder);
            string path = Path.Combine(RecordFolder, file);
            if (!File.Exists(path)) File.WriteAllText(path, header + "\n", new UTF8Encoding(true));   // 엑셀에서 한글이 깨지지 않게 BOM
            File.AppendAllText(path, line + "\n", new UTF8Encoding(false));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[기록 모드] {file}에 저장하지 못했습니다: {e.Message}");
        }
    }

    // 예전 세션들의 (목표 합, 평균 제거)
    private static List<KeyValuePair<int, double>> ReadHistory()
    {
        List<KeyValuePair<int, double>> history = new List<KeyValuePair<int, double>>();
        try
        {
            string path = Path.Combine(RecordFolder, "player_sessions.csv");
            if (!File.Exists(path)) return history;
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] cells = lines[i].Split(',');
                if (cells.Length > 3
                    && int.TryParse(cells[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sum)
                    && double.TryParse(cells[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double removed))
                {
                    history.Add(new KeyValuePair<int, double>(sum, removed));
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[기록 모드] 지난 기록을 읽지 못했습니다: {e.Message}");
        }
        return history;
    }

    // ── 표시 ──────────────────────────────────────────────────

    private void Notify(string message, float seconds)
    {
        notice = message;
        noticeUntil = Time.unscaledTime + seconds;
    }

    private static string ItemNames(IReadOnlyList<SkillType> skills)
    {
        List<string> names = new List<string>();
        foreach (SkillType skill in skills) names.Add(SkillName(skill));
        return string.Join(" ", names);
    }

    private static string SkillName(SkillType skill)
    {
        switch (skill)
        {
            case SkillType.Magnifier: return "돋보기";
            case SkillType.Destroy: return "파괴";
            case SkillType.Transform: return "변신";
            case SkillType.Merge: return "합체";
            case SkillType.Joker: return "조커";
            case SkillType.NumberChange: return "숫자변경";
            default: return skill.ToString();
        }
    }

    // 개발용 상태 표시 (출시 빌드에는 없음)
    private void OnGUI()
    {
        StringBuilder text = new StringBuilder();
        switch (phase)
        {
            case Phase.Asking:
                text.AppendLine("몇 점 기록을 남기시겠습니까?");
                text.AppendLine($"목표 합 ({GameRules.MinTargetSum} ~ {GameRules.MaxTargetSum}): {typed}_");
                text.Append($"숫자 입력 후 Enter → 그 목표 합으로 {GamesPerSession}판 연속   (T · Esc 취소)");
                break;
            case Phase.Recording:
                text.Append($"기록 모드 · 목표 {target} · {Math.Min(logs.Count + 1, GamesPerSession)}/{GamesPerSession}판");
                if (current != null && game != null && game.Board != null)
                    text.Append($" · 제거 {AiSimulator.CountRemoved(game.Board)}마리 · 실수 {current.Misses}");
                if (lastResult != null) text.Append($"\n지난 판 {lastResult}");
                text.Append("\nT 두 번: 기록 중단");
                break;
            case Phase.Done:
                text.AppendLine(report);
                text.Append($"기록: {Path.GetFullPath(RecordFolder)} (player_games.csv, player_sessions.csv)   T 닫기");
                break;
        }

        bool showNotice = notice != null && Time.unscaledTime < noticeUntil;
        if (showNotice)
        {
            if (text.Length > 0) text.Append('\n');
            text.Append(notice);
        }
        if (text.Length == 0) return;

        GUIStyle style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, wordWrap = true };
        GUIContent content = new GUIContent(text.ToString());
        float width = Mathf.Min(phase == Phase.Done ? 900f : 620f, Screen.width - 20f);
        GUI.Box(new Rect(10, 10, width, style.CalcHeight(content, width)), content, style);
    }
}
#endif
