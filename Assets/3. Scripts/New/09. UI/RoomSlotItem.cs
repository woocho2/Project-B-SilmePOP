using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 대기실의 자리 카드 하나. RobbyPanel이 자리 정보를 넘겨 주면 그리기만 하고,
// 방장이 AI 토글이나 난이도를 바꾸면 이벤트로 알림 (실제 변경은 서버를 거쳐 다시 그려짐).
public class RoomSlotItem : MonoBehaviour
{
    [SerializeField] private TMP_Text txt_PlayerName;
    [SerializeField] private TMP_Text txt_PlayerNumber;
    [SerializeField] private TMP_Text txt_AI;
    [Tooltip("카드의 슬라임 (Card_CharacterBG 아래). 자식 Base를 그 자리 플레이어의 색으로 칠함")]
    [SerializeField] private GameObject slime;
    [SerializeField] private Toggle toggle_AI;
    [Tooltip("AI 난이도. 항목은 코드가 EASY, NORMAL, HARD, HELL 순서로 채움")]
    [SerializeField] private TMP_Dropdown dropdown_Difficulty;

    public event Action<int, bool> AiToggled;                   // 자리, 켜짐
    public event Action<int, AiDifficulty> DifficultyChanged;   // 자리, 난이도

    private int slot;
    private SlimeTint tint;

    private void Awake()
    {
        FindMissingParts();
        tint = new SlimeTint(slime);

        if (dropdown_Difficulty != null)
        {
            KeepSingleTemplateItem(dropdown_Difficulty);
            dropdown_Difficulty.ClearOptions();
            dropdown_Difficulty.AddOptions(new List<string> { "EASY", "NORMAL", "HARD", "HELL" });
            dropdown_Difficulty.onValueChanged.AddListener(index => DifficultyChanged?.Invoke(slot, (AiDifficulty)index));
        }
        if (toggle_AI != null)
        {
            toggle_AI.onValueChanged.AddListener(isOn => AiToggled?.Invoke(slot, isOn));
        }
    }

    // isOpen: 방의 최대 인원 안의 자리인가. 닫힌 자리는 카드를 숨김.
    public void Show(int slotIndex, SlotInfo info, bool isOpen, bool isHost)
    {
        slot = slotIndex;
        gameObject.SetActive(isOpen);
        if (!isOpen) return;

        SlotKind kind = info != null ? info.kind : SlotKind.Empty;
        bool isAi = kind == SlotKind.Ai;
        bool hasPlayer = kind != SlotKind.Empty;

        // 빈자리는 슬라임·닉네임·PLAYER 글자를 숨김 (AI 토글은 방장이 AI를 넣을 수 있게 남김)
        if (txt_PlayerNumber != null)
        {
            txt_PlayerNumber.gameObject.SetActive(hasPlayer);
            txt_PlayerNumber.text = $"PLAYER {slotIndex + 1}";
        }
        if (txt_PlayerName != null)
        {
            txt_PlayerName.gameObject.SetActive(hasPlayer);
            if (hasPlayer) txt_PlayerName.text = Nickname(info.displayName);
        }
        if (txt_AI != null) txt_AI.gameObject.SetActive(isAi);

        // 슬라임은 사람 자리에만. AI 자리는 Txt_AI로 표시함
        bool isHuman = kind == SlotKind.Human;
        if (slime != null) slime.SetActive(isHuman);
        if (isHuman) tint.Apply(info.slimeColor);

        // 사람이 있는 자리에는 AI 토글이 없음. 방장만 누를 수 있음.
        if (toggle_AI != null)
        {
            toggle_AI.gameObject.SetActive(kind != SlotKind.Human);
            toggle_AI.SetIsOnWithoutNotify(isAi);
            toggle_AI.interactable = isHost;
        }

        // 난이도는 AI 자리에서만 보임
        if (dropdown_Difficulty != null)
        {
            dropdown_Difficulty.gameObject.SetActive(isAi);
            if (isAi) dropdown_Difficulty.SetValueWithoutNotify((int)info.difficulty);
            dropdown_Difficulty.interactable = isHost;
        }
    }

    // 비운 칸은 카드 안에서 같은 이름의 오브젝트로 찾음 (카드를 복사해 만들 때 연결을 빠뜨려도 동작하게)
    private void FindMissingParts()
    {
        if (txt_PlayerName == null) txt_PlayerName = FindChild("Txt_PlayerName")?.GetComponent<TMP_Text>();
        if (txt_PlayerNumber == null) txt_PlayerNumber = FindChild("Txt_PlayerNumber")?.GetComponent<TMP_Text>();
        if (txt_AI == null) txt_AI = FindChild("Txt_AI")?.GetComponent<TMP_Text>();
        if (slime == null) slime = FindChild("Slime")?.gameObject;
        if (toggle_AI == null) toggle_AI = FindChild("CheckAI")?.GetComponent<Toggle>();
        if (dropdown_Difficulty == null) dropdown_Difficulty = GetComponentInChildren<TMP_Dropdown>(true);
    }

    private Transform FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != transform && child.name == childName) return child;
        }
        return null;
    }

    // TMP_Dropdown은 템플릿의 항목 하나를 본보기로 목록 수만큼 복사함.
    // 템플릿에 항목을 여러 개 만들어 두면 복사본과 겹쳐 하나만 보이므로, 본보기 하나만 남김 (실행 중에만, 씬은 그대로).
    private static void KeepSingleTemplateItem(TMP_Dropdown dropdown)
    {
        if (dropdown.template == null || dropdown.itemText == null) return;

        Toggle sample = dropdown.itemText.GetComponentInParent<Toggle>(true);
        foreach (Toggle item in dropdown.template.GetComponentsInChildren<Toggle>(true))
        {
            if (item != sample) Destroy(item.gameObject);
        }
    }

    // "닉네임#0012" → "닉네임"
    private static string Nickname(string displayName)
    {
        int tag = displayName.LastIndexOf('#');
        return tag > 0 ? displayName.Substring(0, tag) : displayName;
    }
}
