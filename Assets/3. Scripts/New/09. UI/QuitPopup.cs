using UnityEngine;
using UnityEngine.UI;

// 게임 종료 확인 팝업. QuitPanel에 붙임. 지금 화면 위에 겹쳐 뜨고, 아니오를 누르면 팝업만 닫힘.
public class QuitPopup : MonoBehaviour
{
    [SerializeField] private Button btn_no;
    [SerializeField] private Button btn_yes;

    private void Awake()
    {
        UIButtons.Bind(btn_no, () => gameObject.SetActive(false));
        UIButtons.Bind(btn_yes, QuitGame);
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
