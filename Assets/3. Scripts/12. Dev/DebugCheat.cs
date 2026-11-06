#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

// 개발용 키. 에디터와 개발 빌드에서만 있고 출시 빌드에는 들어가지 않음. 씬에 놓지 않아도 저절로 생김.
//   타이틀 T: 글자 등장 연출을 처음부터 다시 재생
//   게임 F1: 목표 합을 10 → 11 → ... → 15 → 10 순서로 바꿈
// 대기실 T(발표용 자동 플레이)는 출시 빌드에도 있어야 해서 AutoPlayInput에 있음.
public class DebugCheat : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject cheat = new GameObject(nameof(DebugCheat));
        cheat.AddComponent<DebugCheat>();
        DontDestroyOnLoad(cheat);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || ChatPanel.IsTyping) return;

        if (keyboard.tKey.wasPressedThisFrame)
        {
            Login_TitlePanel title = FindAnyObjectByType<Login_TitlePanel>();   // 켜져 있는 타이틀만 찾음
            if (title != null) title.ReplayIntro();
        }

        GameManager game = GameManager.Instance;
        if (game != null && game.Board != null && keyboard.f1Key.wasPressedThisFrame)
        {
            int next = game.TargetSum + 1;
            game.SetTargetSumForDebug(next > GameRules.MaxTargetSum ? GameRules.MinTargetSum : next);
        }
    }
}
#endif
