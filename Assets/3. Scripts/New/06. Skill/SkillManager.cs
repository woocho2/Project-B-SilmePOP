using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 내 스킬 6종 (기획 [4-1]). 단축키 1 ~ 6 또는 버튼으로 고르고, 판 위에 적용 범위를 보여 준 뒤
// 클릭으로 적용, ESC(또는 오른쪽 클릭)로 취소 (취소는 횟수를 쓰지 않음). 돋보기는 고르는 순간 바로 씀.
// 채팅 입력 중에는 단축키를 막음. 규칙은 SkillSet(순수 C#)에 있고 여기는 입력과 표시만 함.
public class SkillManager : Singleton<SkillManager>
{
    [Tooltip("스킬 버튼 6개. 순서: 돋보기, 파괴, 변신, 합체, 조커, 숫자변경 (= 단축키 1 ~ 6). 비워 둔 칸은 단축키로만 씀")]
    [SerializeField] private Button[] skillButtons = new Button[6];
    [Tooltip("선택 사항. 남은 횟수 글자 6개 (같은 순서). '남은/최대'로 보여 줌")]
    [SerializeField] private TMP_Text[] usesTexts = new TMP_Text[6];
    [SerializeField] private Color canApplyColor = new Color(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color cannotApplyColor = new Color(1f, 0.2f, 0.2f, 0.9f);

    private static readonly Key[] HotKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
    private static readonly Key[] NumpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6 };

    public SkillSet Skills { get; private set; }
    public SkillType Targeting { get; private set; } = SkillType.None;
    public bool IsTargeting => Targeting != SkillType.None;
    public bool HasAnySkillLeft => Skills != null && Skills.HasAnyLeft;

    // 이번 프레임의 클릭을 스킬 적용에 썼는가. DragManager가 같은 클릭으로 드래그를 시작하지 않게 함.
    public bool ConsumedClickThisFrame => consumedClickFrame == Time.frameCount;

    private int consumedClickFrame = -1;
    private bool wasPlaying;
    private readonly List<int> previewCells = new List<int>();
    private int previewCell = -1;
    private bool previewCanApply;
    private Sequence highlightTween;

    private void Start()
    {
        for (int i = 0; i < skillButtons.Length && i < SkillSet.All.Length; i++)
        {
            SkillType type = SkillSet.All[i];
            UIButtons.Bind(skillButtons[i], () => Select(type));
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.MatchCreated += OnMatchCreated;
            if (GameManager.Instance.Settings != null) OnMatchCreated();
        }
    }

    protected override void OnDestroy()
    {
        highlightTween?.Kill();
        if (GameManager.Instance != null) GameManager.Instance.MatchCreated -= OnMatchCreated;
        base.OnDestroy();
    }

    // 새 판마다 횟수와 스킬 난수를 처음부터
    private void OnMatchCreated()
    {
        CancelTargeting();
        Skills = new SkillSet(GameManager.Instance.Settings.Seed);
        Skills.UsesChanged += (_, _) => RefreshButtons();
        RefreshButtons();
    }

    private void Update()
    {
        GameManager game = GameManager.Instance;
        if (game == null || Skills == null) return;

        // 카운트다운이 끝나거나 판이 끝나면 버튼을 켜고 끔
        if (game.IsPlaying != wasPlaying)
        {
            wasPlaying = game.IsPlaying;
            RefreshButtons();
        }
        if (!game.IsPlaying)
        {
            CancelTargeting();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && !ChatPanel.IsTyping)
        {
            for (int i = 0; i < HotKeys.Length; i++)
            {
                if (keyboard[HotKeys[i]].wasPressedThisFrame || keyboard[NumpadKeys[i]].wasPressedThisFrame) Select(SkillSet.All[i]);
            }
            if (keyboard.escapeKey.wasPressedThisFrame) CancelTargeting();
        }

        if (!IsTargeting || Mouse.current == null) return;

        int cell = MouseCell();
        UpdatePreview(cell);

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelTargeting();
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUi())
        {
            consumedClickFrame = Time.frameCount;
            ApplyTargeting(cell);
        }
    }

    public void Select(SkillType type)
    {
        GameManager game = GameManager.Instance;
        if (game == null || !game.IsPlaying || Skills == null || Skills.UsesLeft(type) <= 0) return;

        if (!Skills.Get(type).NeedsTarget)
        {
            CancelTargeting();
            UseInstant(type);
            return;
        }

        bool sameAsBefore = Targeting == type;
        CancelTargeting();
        if (sameAsBefore) return;   // 같은 스킬을 다시 고르면 취소

        Targeting = type;
        SoundManager.Play(SoundEffect.Click);
    }

    public void CancelTargeting()
    {
        ClearPreview();
        Targeting = SkillType.None;
    }

    // 돋보기: 가능한 조합이 있으면 표시하고 횟수를 씀. 없으면 쓰지 않음.
    private void UseInstant(SkillType type)
    {
        GameManager game = GameManager.Instance;
        if (Skills.TryUse(type, game.Board, -1, game.TargetSum, out _))
        {
            SoundManager.Play(SoundEffect.Hint);
            if (type == SkillType.Magnifier && SlimeManager.Instance != null) SlimeManager.Instance.ShowHint();
        }
        else
        {
            SoundManager.Play(SoundEffect.Miss);
        }
    }

    private void ApplyTargeting(int cell)
    {
        if (cell < 0) return;

        GameManager game = GameManager.Instance;
        SkillType type = Targeting;
        if (!Skills.TryUse(type, game.Board, cell, game.TargetSum, out int score))
        {
            SoundManager.Play(SoundEffect.Miss);   // 쓸 수 없는 곳: 범위는 그대로 두고 다시 고르게 함
            return;
        }

        CancelTargeting();
        SoundManager.Play(type == SkillType.Destroy ? SoundEffect.Destroy : SoundEffect.Mix);
        if (score != 0) game.AddScore(score);
        game.CheckForMoves();
    }

    // ── 범위 미리보기 ───────────────────────────────────────────

    private void UpdatePreview(int cell)
    {
        GameManager game = GameManager.Instance;
        bool canApply = cell >= 0 && Skills.CanUse(Targeting, game.Board, cell, game.TargetSum);
        if (cell == previewCell && canApply == previewCanApply) return;   // 바뀐 게 없으면 다시 칠하지 않음

        ClearPreview();
        previewCell = cell;
        previewCanApply = canApply;
        if (cell < 0 || SlimeManager.Instance == null) return;

        previewCells.AddRange(Skills.Get(Targeting).GetArea(cell));
        SlimeManager.Instance.SetSkillPreview(previewCells, true, canApply ? canApplyColor : cannotApplyColor);
    }

    private void ClearPreview()
    {
        if (previewCells.Count > 0 && SlimeManager.Instance != null)
        {
            SlimeManager.Instance.SetSkillPreview(previewCells, false, Color.clear);
        }
        previewCells.Clear();
        previewCell = -1;
    }

    private static int MouseCell()
    {
        if (SlimeManager.Instance == null || Camera.main == null) return -1;
        Vector2 world = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        return SlimeManager.Instance.CellAt(world);
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // ── 버튼 ────────────────────────────────────────────────────

    private void RefreshButtons()
    {
        bool isPlaying = GameManager.Instance != null && GameManager.Instance.IsPlaying;
        for (int i = 0; i < SkillSet.All.Length; i++)
        {
            SkillType type = SkillSet.All[i];
            int left = Skills != null ? Skills.UsesLeft(type) : 0;

            if (i < skillButtons.Length && skillButtons[i] != null) skillButtons[i].interactable = isPlaying && left > 0;
            if (i < usesTexts.Length && usesTexts[i] != null) usesTexts[i].SetText("{0}/{1}", left, GameRules.SkillUses(type));
        }
    }

    // 판이 막혔을 때 남은 스킬 버튼을 노랗게 세 번 반짝임
    public void HighlightAvailableSkills()
    {
        highlightTween?.Kill();

        List<Button> targets = new List<Button>();
        for (int i = 0; i < skillButtons.Length && i < SkillSet.All.Length; i++)
        {
            if (skillButtons[i] != null && skillButtons[i].image != null && Skills.UsesLeft(SkillSet.All[i]) > 0) targets.Add(skillButtons[i]);
        }
        if (targets.Count == 0) return;

        Color originColor = Color.white;
        Color highlightColor = new Color(1f, 0.9f, 0.3f, 1f);
        highlightTween = DOTween.Sequence();
        foreach (Button button in targets)
        {
            button.image.color = originColor;
            highlightTween.Join(button.image.DOColor(highlightColor, 0.4f).SetEase(Ease.Linear).SetLoops(6, LoopType.Yoyo));
        }
        highlightTween.OnKill(() =>
        {
            foreach (Button button in targets)
            {
                if (button != null && button.image != null) button.image.color = originColor;
            }
        });
    }
}
