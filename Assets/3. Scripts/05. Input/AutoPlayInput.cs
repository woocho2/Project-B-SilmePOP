using UnityEngine;
using UnityEngine.InputSystem;

// 발표용 자동 플레이(AiAutoPlay) 키. 출시 빌드에도 들어감 (발표를 빌드로 하므로). 씬에 놓지 않아도 저절로 생김.
//   대기실 T: 켜기·끄기. 켜면 게임에서 내 자리도 AI가 사람처럼 둠.
//   켜져 있는 동안 1 쉬움 · 2 보통 · 3 어려움 · 4 지옥
// 바꿀 때마다 왼쪽 위에 몇 초만 상태를 보여 줌 (발표 화면을 가리지 않게).
public class AutoPlayInput : MonoBehaviour
{
    private const float NoticeTime = 3f;
    private static readonly AiDifficulty[] DifficultyKeys = { AiDifficulty.Easy, AiDifficulty.Normal, AiDifficulty.Hard, AiDifficulty.Hell };

    private float noticeUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject input = new GameObject(nameof(AutoPlayInput));
        input.AddComponent<AutoPlayInput>().useGUILayout = false;   // GUILayout을 안 쓰므로 매 프레임 레이아웃 단계를 건너뜀
        DontDestroyOnLoad(input);
    }

    private static bool InRoom => UIManager_Room.Instance != null && UIManager_Room.Instance.isActiveAndEnabled;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || ChatPanel.IsTyping || !InRoom) return;

        if (keyboard.tKey.wasPressedThisFrame)
        {
            AiAutoPlay.IsOn = !AiAutoPlay.IsOn;
            noticeUntil = Time.unscaledTime + NoticeTime;
        }
        if (!AiAutoPlay.IsOn) return;

        for (int i = 0; i < DifficultyKeys.Length; i++)
        {
            if (keyboard[Key.Digit1 + i].wasPressedThisFrame || keyboard[Key.Numpad1 + i].wasPressedThisFrame)
            {
                AiAutoPlay.Difficulty = DifficultyKeys[i];
                noticeUntil = Time.unscaledTime + NoticeTime;
            }
        }
    }

    private void OnGUI()
    {
        if (!InRoom || Time.unscaledTime >= noticeUntil) return;

        string text = AiAutoPlay.IsOn
            ? $"내 자리: AI 자동 플레이 ({AiAutoPlay.DifficultyName(AiAutoPlay.Difficulty)})\n1 쉬움 · 2 보통 · 3 어려움 · 4 지옥   T 끄기"
            : "AI 자동 플레이 끔";
        GUIStyle style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14 };
        GUIContent content = new GUIContent(text);
        float width = Mathf.Min(360f, Screen.width - 20f);
        GUI.Box(new Rect(10, 10, width, style.CalcHeight(content, width)), content, style);
    }
}
