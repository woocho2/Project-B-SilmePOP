using UnityEngine;
using UnityEngine.UI;

// 도움말 패널. HelpPanel에 붙임. 닫으면 연 화면으로 돌아감.
public class HelpPanel : MonoBehaviour
{
    [SerializeField] private Button btn_helpBack;

    private void Awake()
    {
        UIButtons.Bind(btn_helpBack, () => UIManager_Lobby.Instance.CloseSubPanel());
    }
}
