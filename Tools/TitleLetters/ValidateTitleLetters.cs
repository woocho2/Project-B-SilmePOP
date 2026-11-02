// Used only in the isolated Temp/TitleLettersPreview project, never in the game Assets folder.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ValidateTitleLetters
{
    const string Active = "TitleLetters.Check.Active";
    static UIManager_Start manager;
    static RectTransform[] letters;
    static Vector2[] positions;
    static Vector3[] scales;
    static Quaternion[] rotations;
    static Button button;
    static Image logo;
    static float began;
    static int phase;
    static int checks;

    static ValidateTitleLetters()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, trace, type) =>
        {
            // Unity 6000.3's background Search index can fail in a fresh headless project.
            if (trace.Contains("UnityEditor.Search.SearchDatabase")) return;
            if (SessionState.GetBool(Active, false) && (type == LogType.Error || type == LogType.Exception))
                Fail(message + "\n" + trace);
        };
    }

    public static void Run()
    {
        SessionState.SetBool(Active, true);
        SessionState.SetFloat("TitleLetters.Check.Deadline", (float)EditorApplication.timeSinceStartup + 90f);
        AssetDatabase.Refresh();
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/SlimePop_TitleLetters.png").OfType<Sprite>().ToArray();
        Check(sprites.Length == 8, "Eight imported sprites");
        foreach (string name in new[] { "S", "L", "I", "M", "E", "P", "O", "Exclamation" })
            Check(sprites.Any(sprite => sprite.name == "Title_" + name), "Sprite " + name);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    static void Set(string name, object value) => typeof(UIManager_Start)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, value);

    static void Setup()
    {
        Time.timeScale = 0f;
        var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var title = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        title.transform.SetParent(canvas.transform, false);
        title.transform.localScale = Vector3.one * 0.7f;
        logo = title.GetComponent<Image>();
        string[] order = { "S", "L", "I", "M", "E", "P", "O", "P", "Exclamation", "Exclamation", "Exclamation" };
        letters = new RectTransform[order.Length];
        positions = new Vector2[order.Length];
        scales = new Vector3[order.Length];
        rotations = new Quaternion[order.Length];
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/SlimePop_TitleLetters.png").OfType<Sprite>().ToArray();
        for (int i = 0; i < order.Length; i++)
        {
            if (order[i] == null) continue;
            var glyph = new GameObject("Glyph_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glyph.transform.SetParent(title.transform, false);
            letters[i] = glyph.GetComponent<RectTransform>();
            glyph.GetComponent<Image>().sprite = sprites.Single(sprite => sprite.name == "Title_" + order[i]);
            positions[i] = letters[i].anchoredPosition = new Vector2(i * 80f, i % 2 * 5f);
            scales[i] = letters[i].localScale = new Vector3(0.8f + i * 0.01f, 0.9f, 1f);
            rotations[i] = letters[i].localRotation = Quaternion.Euler(0f, 0f, i - 5f);
        }
        var start = new GameObject("Start", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        start.transform.SetParent(canvas.transform, false);
        button = start.GetComponent<Button>();
        manager = new GameObject("Manager").AddComponent<UIManager_Start>();
        Set("rect_Logo", title.GetComponent<RectTransform>());
        Set("img_Logo", logo);
        Set("titleLetters", letters);
        Set("rect_GameStart", start.GetComponent<RectTransform>());
        Set("img_GameStart", start.GetComponent<Image>());
        Set("btn_GameStart", button);
        Set("slimeToPopDelay", 0.4f);
        Set("popToExclamationDelay", 0.6f);
        var schedule = typeof(UIManager_Start).GetMethod("TitleLetterStartTime", BindingFlags.NonPublic | BindingFlags.Instance);
        float popTime = (float)schedule.Invoke(manager, new object[] { 5, 0.12f, 0.24f });
        Check(Mathf.Abs(popTime - 1.12f) < 0.001f, "POP waits 0.4 seconds after E settles");
        for (int i = 8; i < 11; i++)
        {
            float punctuationTime = (float)schedule.Invoke(manager, new object[] { i, 0.12f, 0.24f });
            Check(Mathf.Abs(punctuationTime - 2.2f) < 0.001f, "Punctuation shares start after 0.6 second pause " + i);
        }
        began = Time.unscaledTime;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Active, false)) return;
        try
        {
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat("TitleLetters.Check.Deadline", 0f))
                throw new Exception("Play-mode validation timed out");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (manager == null && phase == 0) { Setup(); phase = 1; return; }
            float age = Time.unscaledTime - began;
            if (phase == 1 && letters[0].localScale.sqrMagnitude > 0f)
            {
                Check(!button.interactable, "Start gated during letters");
                Check(!logo.enabled, "Old wordmark hidden");
                Check(letters[0].localScale.sqrMagnitude > 0f, "First glyph animates");
                Check(letters[10].localScale == Vector3.zero, "Final glyph still hidden");
                phase = 2;
            }
            if (phase == 2 && Vector3.Distance(letters[4].localScale, scales[4]) < 0.001f)
            {
                Check(Vector3.Distance(letters[4].localScale, scales[4]) < 0.001f, "SLIME settles before word pause");
                for (int i = 5; i < letters.Length; i++)
                    Check(letters[i].localScale == Vector3.zero, "POP and punctuation wait after SLIME " + i);
                phase = 3;
            }
            if (phase == 3 && Vector3.Distance(letters[7].localScale, scales[7]) < 0.001f)
            {
                Check(Vector3.Distance(letters[7].localScale, scales[7]) < 0.001f, "POP settles before punctuation pause");
                for (int i = 8; i < letters.Length; i++)
                    Check(letters[i].localScale == Vector3.zero, "Punctuation waits after POP " + i);
                phase = 6;
            }
            if (phase == 6 && letters[8].localScale.sqrMagnitude > 0f)
            {
                Vector2 referenceScale = new Vector2(letters[8].localScale.x / scales[8].x, letters[8].localScale.y / scales[8].y);
                for (int i = 8; i < letters.Length; i++)
                {
                    Check(letters[i].localScale.sqrMagnitude > 0f, "Punctuation appears together " + i);
                    Vector2 normalizedScale = new Vector2(letters[i].localScale.x / scales[i].x, letters[i].localScale.y / scales[i].y);
                    Check(Vector2.Distance(referenceScale, normalizedScale) < 0.001f, "Punctuation shares animation progress " + i);
                }
                phase = 7;
            }
            if (phase == 7 && button.interactable)
            {
                CheckFinalPose();
                manager.StopTwinkle();
                Check(!button.GetComponent<Image>().enabled, "Stopped twinkle hides start image");
                manager.ReplayIntro();
                Check(!button.interactable, "Replay gates start again");
                Check(button.GetComponent<Image>().enabled, "Replay restores start image");
                Check(letters[10].localScale == Vector3.zero, "Replay hides later glyphs");
                began = Time.unscaledTime;
                phase = 4;
            }
            else if (phase == 4 && age > 0.1f)
            {
                Check(!button.interactable, "Start stays gated during replay");
                // Repeated presses during the approach must not capture the enlarged pose.
                manager.ReplayIntro();
                manager.ReplayIntro();
                Check(letters[10].localScale == Vector3.zero, "Repeated replay restarts glyph sequence");
                began = Time.unscaledTime;
                phase = 5;
            }
            else if (phase == 5 && button.interactable)
            {
                CheckFinalPose();
                manager.enabled = false;
                Check(Time.timeScale == 0f, "Animation completes with scaled time paused");
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Tools/TitleLetters/Validation.txt"));
                File.WriteAllText(output, "PASS: Unity imports 8 named sprites; 11 glyphs in SLIME / POP / !!! order; independently configured pauses after E and the final P settle; all three exclamation marks appear simultaneously with identical animation progress; old logo hidden; start gating; replay after completion; repeated replay during animation; start image restored after StopTwinkle; authored position/scale/rotation/sibling order restored without drift; completes at Time.timeScale=0. Checks: " + checks);
                SessionState.SetBool(Active, false);
                EditorApplication.Exit(0);
            }
        }
        catch (Exception exception) { Fail(exception.ToString()); }
    }

    static void CheckFinalPose()
    {
        Check(button.interactable, "Start enabled after final glyph and button");
        for (int i = 0; i < letters.Length; i++)
        {
            if (letters[i] == null) continue;
            Check(Vector2.Distance(letters[i].anchoredPosition, positions[i]) < 0.001f, "Position restored " + i);
            Check(Vector3.Distance(letters[i].localScale, scales[i]) < 0.001f, "Scale restored " + i);
            Check(Quaternion.Angle(letters[i].localRotation, rotations[i]) < 0.001f, "Rotation restored " + i);
            Check(letters[i].GetSiblingIndex() == i, "Sibling order retained " + i);
        }
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    static void Fail(string message)
    {
        SessionState.SetBool(Active, false);
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Tools/TitleLetters/Validation.txt")), "FAIL: " + message);
        EditorApplication.Exit(1);
    }
}
