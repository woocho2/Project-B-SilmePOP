#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

// 개발용 키. 에디터와 개발 빌드에서만 있고 출시 빌드에는 들어가지 않음. 씬에 놓지 않아도 저절로 생김.
//   F1: 목표 합을 10 → 11 → ... → 15 → 10 순서로 바꿈
//   T: 타이틀 화면의 글자 등장 연출을 처음부터 다시 재생
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
        if (Keyboard.current == null) return;

        if (Keyboard.current.tKey.wasPressedThisFrame && UIManager_Start.Instance != null)
            UIManager_Start.Instance.ReplayIntro();

        if (GameManager.Instance == null || GameManager.Instance.Board == null) return;

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            int next = GameManager.Instance.TargetSum + 1;
            GameManager.Instance.SetTargetSumForDebug(next > GameRules.MaxTargetSum ? GameRules.MinTargetSum : next);
        }
    }
}
#endif
