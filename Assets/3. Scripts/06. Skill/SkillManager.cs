using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 이번 판의 아이템(스킬) 칸 (기획 [4-1]). 6종 중 판마다 뽑힌 GameRules.SkillsPerMatch개만 씀 (MatchSettings.Skills, 모두 같음).
// 씬에 칸이 더 있으면(Skill_item2, 3 ...) 숨김.
// 단축키 A(, S, D) 또는 칸 클릭으로 고르고, 판 위에 적용 범위를 보여 준 뒤 클릭으로 적용,
// ESC(또는 오른쪽 클릭)로 취소 (취소는 횟수를 쓰지 않음). 돋보기·조커는 고르는 순간 바로 씀 (자리를 고르지 않음).
// 칸은 시작 전 아이템 뽑기(Game_ReadyPanel)가 하나씩 RevealSlot으로 채움. 뽑기가 없으면 시작할 때 한 번에 채움.
// 채팅 입력 중에는 단축키를 막음. 규칙은 SkillSet(순수 C#)에 있고 여기는 입력과 표시만 함.
public class SkillManager : Singleton<SkillManager>
{
    private const int SlotCount = GameRules.SkillsPerMatch;

    [Tooltip("칸 그림 (Skill_item1 ~). 뽑힌 아이템 그림으로 바뀜. 비우면 이름으로 찾음")]
    [SerializeField] private Image[] slotIcons = new Image[SlotCount];
    [Tooltip("남은 횟수 글자 (txt_item1 ~). 비우면 이름으로 찾음")]
    [SerializeField] private TMP_Text[] usesTexts = new TMP_Text[SlotCount];
    [SerializeField] private Color canApplyColor = new Color(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color cannotApplyColor = new Color(1f, 0.2f, 0.2f, 0.9f);
    [Tooltip("아직 안 뽑힌 칸의 색")]
    [SerializeField] private Color hiddenSlotColor = new Color(1f, 1f, 1f, 0.15f);

    private static readonly Key[] HotKeys = { Key.A, Key.S, Key.D };

    public SkillSet Skills { get; private set; }
    public SkillType Targeting { get; private set; } = SkillType.None;
    public bool IsTargeting => Targeting != SkillType.None;
    public bool HasAnySkillLeft => Skills != null && Skills.HasAnyLeft;

    // 이번 프레임의 클릭을 스킬 적용에 썼는가. DragManager가 같은 클릭으로 드래그를 시작하지 않게 함.
    public bool ConsumedClickThisFrame => consumedClickFrame == Time.frameCount;

    private int consumedClickFrame = -1;
    private bool wasPlaying;
    private readonly bool[] revealed = new bool[SlotCount];
    private readonly Button[] slotButtons = new Button[SlotCount];
    private readonly List<int> previewCells = new List<int>();
    private int previewCell = -1;
    private bool previewCanApply;
    private Sequence highlightTween;

    private void Start()
    {
        FindMissingParts();
        HideUnusedSlots();
        for (int i = 0; i < SlotCount; i++)
        {
            int slot = i;
            if (slotIcons[i] == null) continue;

            // 칸 그림을 누를 수 있게 버튼을 붙임 (씬에 이미 있으면 그것을 씀)
            Button button = slotIcons[i].GetComponent<Button>();
            if (button == null)
            {
                button = slotIcons[i].gameObject.AddComponent<Button>();
                button.targetGraphic = slotIcons[i];
                button.transition = Selectable.Transition.None;   // 색은 RefreshButtons가 칠함
            }
            slotButtons[i] = button;
            UIButtons.Bind(button, () => SelectSlot(slot));
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

    // 씬에 SkillManager를 놓지 않아도 GameManager가 만들어 주므로, 칸은 이름으로 찾음.
    // 칸이 1개뿐이면 게임 패널(Game_GamePanel) 아래의 Item / txt_item도 칸으로 봄 (0.2.4에서 씬 이름이 이렇게 바뀜).
    // 준비 패널에도 Item이라는 오브젝트가 있으므로 게임 패널 아래에서만 찾음.
    private void FindMissingParts()
    {
        Game_GamePanel gamePanel = FindAnyObjectByType<Game_GamePanel>(FindObjectsInactive.Include);
        for (int i = 0; i < SlotCount; i++)
        {
            if (slotIcons[i] == null) slotIcons[i] = FindByName<Image>($"Skill_item{i + 1}");
            if (usesTexts[i] == null) usesTexts[i] = FindByName<TMP_Text>($"txt_item{i + 1}");
            if (SlotCount != 1 || gamePanel == null) continue;

            if (slotIcons[i] == null) slotIcons[i] = FindChildByName<Image>(gamePanel.transform, "Item");
            if (usesTexts[i] == null) usesTexts[i] = FindChildByName<TMP_Text>(gamePanel.transform, "txt_item");
        }
        if (slotIcons[0] == null) Debug.LogWarning("SkillManager: 게임 화면의 아이템 칸(Skill_item1 또는 게임 패널의 Item)을 찾지 못했습니다.");
    }

    private static T FindChildByName<T>(Transform root, string objectName) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.name == objectName) return component;
        }
        return null;
    }

    // 판마다 쓰는 칸 수보다 씬에 칸이 더 있으면 숨김 (횟수 글자는 칸 안에 있지만 따로 있어도 숨김)
    private void HideUnusedSlots()
    {
        for (int i = SlotCount; ; i++)
        {
            Image icon = i < slotIcons.Length && slotIcons[i] != null ? slotIcons[i] : FindByName<Image>($"Skill_item{i + 1}");
            TMP_Text uses = i < usesTexts.Length && usesTexts[i] != null ? usesTexts[i] : FindByName<TMP_Text>($"txt_item{i + 1}");
            if (icon == null && uses == null) return;

            if (icon != null) icon.gameObject.SetActive(false);
            if (uses != null) uses.gameObject.SetActive(false);
        }
    }

    private static T FindByName<T>(string objectName) where T : Component
    {
        foreach (T component in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (component.name == objectName) return component;
        }
        return null;
    }

    // 새 판마다 이번 판의 3개로 횟수와 스킬 난수를 처음부터. 칸은 뽑기가 채울 때까지 가려 둠.
    private void OnMatchCreated()
    {
        CancelTargeting();
        MatchSettings settings = GameManager.Instance.Settings;
        Skills = new SkillSet(settings.Seed, settings.Skills);
        Skills.UsesChanged += (_, _) => RefreshButtons();
        for (int i = 0; i < SlotCount; i++) revealed[i] = false;
        RefreshButtons();
    }

    // 아이템 뽑기가 i번째 칸을 정함: 그림을 바꾸고 살짝 튀게 함. icon이 없으면 칸 그림은 그대로.
    public void RevealSlot(int slot, Sprite icon)
    {
        if (slot < 0 || slot >= SlotCount) return;
        revealed[slot] = true;

        Image image = slotIcons[slot];
        if (image != null)
        {
            if (icon != null) image.sprite = icon;
            image.rectTransform.DOKill(true);
            image.rectTransform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.6f).SetUpdate(true);
        }
        RefreshButtons();
    }

    private void Update()
    {
        GameManager game = GameManager.Instance;
        if (game == null || Skills == null) return;

        // 카운트다운이 끝나거나 판이 끝나면 버튼을 켜고 끔. 시작했는데 아직 안 채운 칸(뽑기 없음)은 채움.
        if (game.IsPlaying != wasPlaying)
        {
            wasPlaying = game.IsPlaying;
            if (wasPlaying)
            {
                for (int i = 0; i < SlotCount; i++) revealed[i] = true;
            }
            RefreshButtons();
        }
        if (!game.IsPlaying)
        {
            CancelTargeting();
            return;
        }
        if (AiAutoPlay.IsOn) return;   // AI가 대신 씀 (AiUse). 내 단축키·마우스는 쓰지 않음

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && !ChatPanel.IsTyping)
        {
            for (int i = 0; i < HotKeys.Length && i < SlotCount; i++)
            {
                if (keyboard[HotKeys[i]].wasPressedThisFrame) SelectSlot(i);
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

    private SkillType SlotSkill(int slot)
    {
        return Skills != null && slot >= 0 && slot < Skills.Available.Count ? Skills.Available[slot] : SkillType.None;
    }

    public void SelectSlot(int slot)
    {
        SkillType type = SlotSkill(slot);
        if (type != SkillType.None) Select(type);
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

    // 자동 플레이(AiAutoPlay): AI가 사람처럼 아이템을 씀. 칸을 반짝이고, 대상이 필요한 아이템은
    // 범위를 previewTime 동안 보여 준 뒤 그 칸에 적용. 끝나면 onDone (판이 끝나 못 썼어도 부름).
    public void AiUse(SkillType type, int cell, float previewTime, System.Action onDone)
    {
        GameManager game = GameManager.Instance;
        if (game == null || !game.IsPlaying || Skills == null || Skills.UsesLeft(type) <= 0)
        {
            onDone?.Invoke();
            return;
        }

        int slot = -1;
        for (int i = 0; i < SlotCount; i++)
        {
            if (SlotSkill(i) == type) slot = i;
        }
        if (slot >= 0 && slotIcons[slot] != null)
        {
            slotIcons[slot].rectTransform.DOKill(true);
            slotIcons[slot].rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.6f);
        }

        if (!Skills.Get(type).NeedsTarget)
        {
            CancelTargeting();
            UseInstant(type);
            onDone?.Invoke();
            return;
        }

        CancelTargeting();
        Targeting = type;
        SoundManager.Play(SoundEffect.Click);
        UpdatePreview(cell);
        DOVirtual.DelayedCall(previewTime, () =>
        {
            if (Targeting == type && GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                ApplyTargeting(cell);
                CancelTargeting();   // 못 쓰는 칸이었으면 (그사이 판이 바뀜) 실패 소리만 내고 그만둠
            }
            onDone?.Invoke();
        }).SetLink(gameObject);
    }

    // 고르는 순간 바로 쓰는 아이템. 쓸 수 없으면(조합·기본 슬라임이 없음) 횟수를 쓰지 않음.
    //   돋보기: 악마 없는 아무 조합 하나를 표시
    //   조커  : 무작위 기본 슬라임 하나가 조커가 됨. 어디인지 잠깐 표시
    private void UseInstant(SkillType type)
    {
        GameManager game = GameManager.Instance;
        if (!Skills.TryUse(type, game.Board, -1, game.TargetSum, out int score))
        {
            SoundManager.Play(SoundEffect.Miss);
            return;
        }

        SlimeManager view = SlimeManager.Instance;
        if (type == SkillType.Magnifier)
        {
            SoundManager.Play(SoundEffect.Hint);
            if (view != null) view.ShowHint(((MagnifierSkill)Skills.Get(type)).LastHint);
        }
        else
        {
            SoundManager.Play(SoundEffect.Mix);
            if (type == SkillType.Joker && view != null) view.FlashCell(((JokerSkill)Skills.Get(type)).LastCell, canApplyColor, JokerFlashTime);
        }
        if (score != 0) game.AddScore(score);
        game.CheckForMoves();
    }

    private const float JokerFlashTime = 1.2f;

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
        if (score != 0 && SlimeManager.Instance != null) SlimeManager.Instance.ShowScore(Skills.Get(type).GetArea(cell), score);
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

    // 버튼처럼 누를 수 있는 UI 위인가 (아이템 칸을 누른 것은 판 클릭이 아님).
    // 화면 전체를 덮는 투명한 배경 이미지 같은 것은 판 클릭을 막지 않음.
    private static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private static bool IsPointerOverUi()
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointer = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);
        foreach (RaycastResult hit in uiHits)
        {
            if (hit.gameObject.GetComponentInParent<Selectable>() != null) return true;
        }
        return false;
    }

    // ── 칸 ──────────────────────────────────────────────────────

    // 안 뽑힌 칸은 흐리게, 다 쓴 칸은 회색, 쓸 수 있는 칸은 원래 색
    private void RefreshButtons()
    {
        bool isPlaying = GameManager.Instance != null && GameManager.Instance.IsPlaying;
        for (int i = 0; i < SlotCount; i++)
        {
            SkillType type = SlotSkill(i);
            int left = type != SkillType.None ? Skills.UsesLeft(type) : 0;

            if (slotButtons[i] != null) slotButtons[i].interactable = isPlaying && left > 0;
            if (slotIcons[i] != null)
            {
                slotIcons[i].color = !revealed[i] ? hiddenSlotColor
                                   : left > 0 ? Color.white
                                   : new Color(0.5f, 0.5f, 0.5f, 0.6f);
            }
            if (usesTexts[i] != null) usesTexts[i].text = revealed[i] ? left.ToString() : "?";
        }
    }

    // 판이 막혔을 때 남은 스킬 칸을 노랗게 세 번 반짝임
    public void HighlightAvailableSkills()
    {
        highlightTween?.Kill();

        List<Image> targets = new List<Image>();
        for (int i = 0; i < SlotCount; i++)
        {
            SkillType type = SlotSkill(i);
            if (slotIcons[i] != null && type != SkillType.None && Skills.UsesLeft(type) > 0) targets.Add(slotIcons[i]);
        }
        if (targets.Count == 0) return;

        Color highlightColor = new Color(1f, 0.9f, 0.3f, 1f);
        highlightTween = DOTween.Sequence();
        foreach (Image image in targets)
        {
            image.color = Color.white;
            highlightTween.Join(image.DOColor(highlightColor, 0.4f).SetEase(Ease.Linear).SetLoops(6, LoopType.Yoyo));
        }
        highlightTween.OnKill(RefreshButtons);
    }
}
