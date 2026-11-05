using UnityEngine.Events;
using UnityEngine.UI;

// 버튼 연결 공통: 누르면 클릭 소리 + 할 일. 인스펙터의 OnClick에 걸린 것은 지우고 코드로만 연결함.
public static class UIButtons
{
    public static void Bind(Button button, UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SoundManager.Play(SoundEffect.Click));
        button.onClick.AddListener(action);
    }

    public static void SetInteractable(Button button, bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }
}
