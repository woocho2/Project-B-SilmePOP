using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메뉴의 방 목록 한 줄 (RoomItem 프리팹). 누르면 그 방에 들어감.
public class RoomListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text txt_Title;
    [SerializeField] private TMP_Text txt_Personnel;
    [SerializeField] private TMP_Text txt_RoomCondition;
    [Tooltip("방장의 슬라임 색으로 칠할 슬라임 (Txt_Personnel 아래)")]
    [SerializeField] private GameObject slime;
    [SerializeField] private Button button;

    public event Action<string> Clicked;   // 방 ID

    private string roomId;
    private SlimeTint tint;

    private void Awake()
    {
        tint = new SlimeTint(slime);
        if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(roomId));
    }

    public void Show(RoomInfo room)
    {
        roomId = room.roomId;
        bool isWaiting = room.state == RoomState.Waiting;
        bool isFull = room.FindEmptySlot() < 0;

        if (txt_Title != null) txt_Title.text = room.roomName;
        if (txt_Personnel != null) txt_Personnel.text = $"{room.PlayerCount}/{room.maxPlayers}";
        if (txt_RoomCondition != null) txt_RoomCondition.text = !isWaiting ? "게임중" : isFull ? "만석" : "대기중";

        int hostSlot = room.FindSlot(room.hostUid);
        if (hostSlot >= 0) tint.Apply(room.slots[hostSlot].slimeColor);

        if (button != null) button.interactable = isWaiting && !isFull;
    }
}
