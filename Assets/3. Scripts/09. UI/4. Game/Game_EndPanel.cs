using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 결과 화면 (GameEndPanel에 붙임. 안 붙여도 UIManager_Game이 이름 GameEndPanel을 찾아 붙여 줌).
// 1 ~ 4등 칸(Rank.1 ~ Rank.4)마다 닉네임(Rank.N Name), 점수(Rank.N Score), 최종 판(Rank.N Board, 검은 칸)을 채움.
// 내 줄의 글자는 게임 화면 순위처럼 파란 그라데이션. 참가자가 없는 칸은 숨김.
// 룸으로 가기 버튼을 누르거나 autoReturnSeconds가 지나면 onReturn (씬 이동은 UIManager_Game).
// 점수·판이 늦게 와도(다른 PC가 조금 늦게 끝남) 오는 대로 다시 그림.
public class Game_EndPanel : MonoBehaviour
{
    [Tooltip("룸으로 가기 버튼. 비우면 이 패널 안의 첫 버튼")]
    [SerializeField] private Button btn_room;
    [Tooltip("남은 시간을 보여 줄 글자 (선택 사항). 비우면 이름 Txt_Countdown, 그것도 없으면 버튼 글자 뒤에 (20)처럼 붙임")]
    [SerializeField] private TMP_Text txt_countdown;
    [SerializeField, Min(1f)] private float autoReturnSeconds = 20f;
    [SerializeField] private string scoreFormat = "Score : {0}";
    [Tooltip("내 이름·점수 색 (게임 화면 순위와 같은 파랑)")]
    [SerializeField] private Color myTextColor = new Color(0.2f, 0.45f, 0.9f);

    private sealed class Group
    {
        public GameObject root;
        public TMP_Text name;
        public TMP_Text score;
        public RectTransform board;
        public int shownSlot = -1;
        public BoardSnapshot shownBoard;
    }

    private sealed class TextLook
    {
        public Color color;
        public bool gradient;
        public VertexGradient colors;
        public FontStyles style;
    }

    private readonly List<Group> groups = new List<Group>();
    private readonly Dictionary<TMP_Text, TextLook> looks = new Dictionary<TMP_Text, TextLook>();
    private readonly Dictionary<int, BoardSnapshot> boards = new Dictionary<int, BoardSnapshot>();
    private List<ScoreEntry> ranking = new List<ScoreEntry>();
    private TMP_Text buttonLabel;
    private string buttonText;
    private Sequence countdown;
    private Action onReturn;
    private bool subscribed;
    private bool found;

    private void FindParts()
    {
        if (found) return;
        found = true;

        for (int n = 1; n <= GameRules.MaxPlayers; n++)
        {
            Transform root = FindDeep(transform, $"Rank.{n}");
            if (root == null) continue;

            Group group = new Group
            {
                root = root.gameObject,
                name = FindDeep(root, $"Rank.{n} Name")?.GetComponent<TMP_Text>(),
                score = FindDeep(root, $"Rank.{n} Score")?.GetComponent<TMP_Text>(),
                board = FindDeep(root, $"Rank.{n} Board") as RectTransform
            };
            Remember(group.name);
            Remember(group.score);
            groups.Add(group);
        }
        if (groups.Count == 0) Debug.LogWarning("Game_EndPanel: Rank.1 ~ Rank.4를 찾지 못했습니다.", this);

        if (btn_room == null) btn_room = GetComponentInChildren<Button>(true);
        if (btn_room != null) UIButtons.Bind(btn_room, Return);

        if (txt_countdown == null) txt_countdown = FindDeep(transform, "Txt_Countdown")?.GetComponent<TMP_Text>();
        if (txt_countdown == null && btn_room != null)
        {
            buttonLabel = btn_room.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null) buttonText = buttonLabel.text;
        }
    }

    // 결과를 그리고 남은 시간을 세기 시작. 버튼이나 시간이 다 되면 onReturnToRoom (한 번만).
    public void Show(Action onReturnToRoom)
    {
        FindParts();
        onReturn = onReturnToRoom;
        if (btn_room != null) btn_room.interactable = true;

        boards.Clear();
        NetworkManager network = NetworkManager.Instance;
        if (network != null)
        {
            foreach (KeyValuePair<int, BoardSnapshot> pair in network.FinalBoards) boards[pair.Key] = pair.Value;
        }
        // 내 판은 서버를 거치지 않고 바로
        GameManager game = GameManager.Instance;
        if (game != null && game.Board != null) boards[MySlot()] = BoardSnapshot.FromBoard(game.Board, 0, "");

        ranking = Game_RankPanel.CurrentRanking();
        foreach (Group group in groups)
        {
            group.shownSlot = -1;
            group.shownBoard = null;
        }
        Redraw();
        Subscribe(true);
        StartCountdown();
    }

    private void OnDisable()
    {
        Subscribe(false);
        countdown?.Kill();
        countdown = null;
    }

    private void Subscribe(bool on)
    {
        NetworkManager network = NetworkManager.Instance;
        if (network == null || subscribed == on) return;
        subscribed = on;

        if (on)
        {
            network.ScoresChanged += OnScoresChanged;
            network.FinalBoardReceived += OnFinalBoardReceived;
        }
        else
        {
            network.ScoresChanged -= OnScoresChanged;
            network.FinalBoardReceived -= OnFinalBoardReceived;
        }
    }

    // 먼저 돌아간 사람이 방을 대기 상태로 돌리면 점수가 비워짐 → 그때는 마지막 결과를 그대로 둠
    private void OnScoresChanged(MatchScores scores)
    {
        NetworkManager network = NetworkManager.Instance;
        if (scores.GetRanking().Count == 0 || network == null || network.CurrentMatch == null) return;
        ranking = Game_RankPanel.CurrentRanking();
        Redraw();
    }

    private void OnFinalBoardReceived(int slot, BoardSnapshot snapshot)
    {
        if (slot == MySlot()) return;   // 내 판은 이미 가지고 있음
        boards[slot] = snapshot;
        Redraw();
    }

    // ── 그리기 ──────────────────────────────────────────────────

    private void Redraw()
    {
        for (int rank = 0; rank < groups.Count; rank++)
        {
            Group group = groups[rank];
            bool hasPlayer = rank < ranking.Count;
            group.root.SetActive(hasPlayer);
            if (!hasPlayer) continue;

            ScoreEntry entry = ranking[rank];
            SlotInfo info = SlotOf(entry.slot);
            bool isMe = entry.slot == MySlot();

            if (group.name != null)
            {
                group.name.text = NameOf(info);
                Style(group.name, isMe);
            }
            if (group.score != null)
            {
                group.score.text = string.Format(scoreFormat, entry.forfeited ? "기권" : entry.score.ToString());
                Style(group.score, isMe);
            }

            boards.TryGetValue(entry.slot, out BoardSnapshot snapshot);
            if (group.shownSlot != entry.slot || group.shownBoard != snapshot)
            {
                group.shownSlot = entry.slot;
                group.shownBoard = snapshot;
                TMP_FontAsset font = group.score != null ? group.score.font : null;
                if (snapshot != null) FinalBoardView.Draw(group.board, snapshot, ColorOf(info), font);
                else FinalBoardView.Clear(group.board);
            }
        }
    }

    private static int MySlot()
    {
        NetworkManager network = NetworkManager.Instance;
        return network != null && network.CurrentRoom != null && network.MySlot >= 0 ? network.MySlot : 0;
    }

    private static SlotInfo SlotOf(int slot)
    {
        NetworkManager network = NetworkManager.Instance;
        RoomInfo room = network != null ? network.CurrentRoom : null;
        return room != null && slot >= 0 && slot < room.slots.Length ? room.slots[slot] : null;
    }

    private static string NameOf(SlotInfo info)
    {
        if (info != null && !string.IsNullOrEmpty(info.displayName)) return TextLines.Nickname(info.displayName);
        NetworkManager network = NetworkManager.Instance;
        return network != null && network.CurrentUser != null ? network.CurrentUser.nickname : "나";
    }

    private static Color ColorOf(SlotInfo info)
    {
        if (info != null && ColorUtility.TryParseHtmlString(info.slimeColor, out Color color)) return color;
        return StyleSettings.SelectedColor;
    }

    // 내 글자는 위가 밝은 파란 그라데이션 + 굵게, 다른 사람은 씬에 둔 원래 모습
    private void Style(TMP_Text text, bool isMe)
    {
        if (isMe)
        {
            Color top = Color.Lerp(myTextColor, Color.white, 0.55f);
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(top, top, myTextColor, myTextColor);
            text.fontStyle |= FontStyles.Bold;
            return;
        }

        if (!looks.TryGetValue(text, out TextLook look)) return;
        text.color = look.color;
        text.enableVertexGradient = look.gradient;
        text.colorGradient = look.colors;
        text.fontStyle = look.style;
    }

    private void Remember(TMP_Text text)
    {
        if (text == null || looks.ContainsKey(text)) return;
        looks[text] = new TextLook { color = text.color, gradient = text.enableVertexGradient, colors = text.colorGradient, style = text.fontStyle };
    }

    // ── 룸으로 ──────────────────────────────────────────────────

    private void StartCountdown()
    {
        countdown?.Kill();
        int total = Mathf.CeilToInt(autoReturnSeconds);
        countdown = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        for (int left = total; left >= 1; left--)
        {
            int seconds = left;
            countdown.InsertCallback(total - left, () => ShowSecondsLeft(seconds));
        }
        countdown.InsertCallback(total, Return);
    }

    private void ShowSecondsLeft(int seconds)
    {
        if (txt_countdown != null) txt_countdown.text = $"{seconds}초 뒤 룸으로 돌아갑니다";
        else if (buttonLabel != null) buttonLabel.text = $"{buttonText} ({seconds})";
    }

    private void Return()
    {
        if (onReturn == null) return;   // 이미 돌아가는 중
        Action action = onReturn;
        onReturn = null;
        countdown?.Kill();
        countdown = null;
        if (btn_room != null) btn_room.interactable = false;
        action();
    }

    private static Transform FindDeep(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name == childName) return child;
        }
        return null;
    }
}
