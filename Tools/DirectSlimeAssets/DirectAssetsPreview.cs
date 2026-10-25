using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DirectAssetsPreview
{
    const string Key = "DirectAssetsPreview.Active";
    const string Output = @"C:\Users\Public\Documents\ESTsoft\CreatorTemp\slime-direct-assets\verification";
    static readonly string[] Themes = { "Basic", "Spring", "Summer", "Autumn", "Winter", "Sunny", "Night", "Cloudy" };
    static readonly List<string> Errors = new List<string>();
    static DirectAssetsPreview()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (m, trace, type) => {
            if (!trace.Contains("UnityEditor.Search.SearchDatabase") && (type == LogType.Error || type == LogType.Exception)) Errors.Add(m + "\n" + trace);
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Key, true);
        SessionState.SetInt("Direct.Index", 0);
        SessionState.SetInt("Direct.Phase", 0);
        SessionState.SetFloat("Direct.When", (float)EditorApplication.timeSinceStartup + 8f);
        EditorSceneManager.OpenScene("Assets/1. Scenes/1. GameStart.unity");
        EditorApplication.isPlaying = true;
    }
    static ThemeData Theme(int i) => AssetDatabase.LoadAssetAtPath<ThemeData>("Assets/2. Prefab/" + (i + 1) + ". " + Themes[i] + ".asset");
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
    static void Map(object manager, ThemeData theme) => manager.GetType().GetMethod("ChangeMap", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(manager, new object[] { theme });
    static void Next(int phase, float delay = 0.5f) { SessionState.SetInt("Direct.Phase", phase); SessionState.SetFloat("Direct.When", (float)EditorApplication.timeSinceStartup + delay); }
    static void Capture(string name) => typeof(DoodlePreview).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { name });
    static void CheckArt()
    {
        foreach (var image in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (image.sprite != null) {
                string path = AssetDatabase.GetAssetPath(image.sprite);
                Check(path.StartsWith("Assets/4. SlimeAsset/") || path.StartsWith("Assets/FlexibleColorPicker/") || !path.StartsWith("Assets/"), "UI outside SlimeAsset: " + image.name + " " + path);
            }
        foreach (var sprite in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (sprite.sprite != null) {
                string path = AssetDatabase.GetAssetPath(sprite.sprite);
                Check(path.StartsWith("Assets/4. SlimeAsset/") || !path.StartsWith("Assets/"), "Renderer outside SlimeAsset: " + sprite.name + " " + path);
            }
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || EditorApplication.timeSinceStartup < SessionState.GetFloat("Direct.When", 0)) return;
        try {
            Check(Errors.Count == 0, "Runtime errors: " + string.Join("\n", Errors));
            int i = SessionState.GetInt("Direct.Index", 0);
            switch (SessionState.GetInt("Direct.Phase", 0)) {
                case 0:
                    Check(Resources.LoadAll<Sprite>("DoodleLab").Length == 0, "Skin still supplied through Resources");
                    var catalog = AssetDatabase.LoadAssetAtPath<DoodleSkinAssets>("Assets/4. SlimeAsset/SlimeSkinAssets.asset");
                    Check(catalog.sprites.Length == 66, "66 catalog entries");
                    foreach (var binding in catalog.sprites) {
                        Check(binding.sprite != null, "Missing catalog sprite: " + binding.name);
                        Check(DoodleLabSkin.Sprite(binding.name) == binding.sprite, "Unregistered sprite: " + binding.name + "; loaders=" + UnityEngine.Object.FindObjectsByType<DoodleSkinBindings>(FindObjectsSortMode.None).Length);
                    }
                    for (int t = 0; t < 8; t++) {
                        var theme = Theme(t);
                        string prefix = "Assets/4. SlimeAsset/Map/";
                        Check(AssetDatabase.GetAssetPath(theme.MenuSprite) == prefix + "Menu/" + Themes[t] + "_Menu.png", "Original menu reference");
                        Check(AssetDatabase.GetAssetPath(theme.TableSprite) == prefix + "Table/" + Themes[t] + "_Table.png", "Original table reference");
                        Check(AssetDatabase.GetAssetPath(theme.SlimeSprite) == prefix + "Slime/Slime_" + Themes[t] + ".png", "Original mascot reference");
                        Check(AssetDatabase.GetAssetPath(theme.BarSprite) == prefix + "Bar/" + Themes[t] + "_Bar.png", "Original bar reference " + Themes[t] + ": " + AssetDatabase.GetAssetPath(theme.BarSprite) + "; " + JsonUtility.ToJson(theme));
                        Check(theme.BarSprite.border.x > 0 && theme.TableSprite.border.x > 0, "Original nine-slice borders");
                    }
                    CheckArt(); Capture("direct-start");
                    SceneManager.LoadScene("2. GameMenu"); Next(1, 5f); break;
                case 1:
                    Map(UIManager_Menu.Instance, Theme(i)); Next(2); break;
                case 2:
                    Check(Field<GameObject>(UIManager_Menu.Instance, "Panel_MainMenu").GetComponent<Image>().sprite == Theme(i).MenuSprite, "Live original menu");
                    CheckArt(); Capture("direct-menu-" + Themes[i]);
                    if (++i < 8) { SessionState.SetInt("Direct.Index", i); Next(1, 0.1f); }
                    else { Field<Button>(UIManager_Menu.Instance, "btn_option").onClick.Invoke(); Next(3); }
                    break;
                case 3:
                    Check(Field<GameObject>(UIManager_Menu.Instance, "Panel_Option").activeSelf, "Options open");
                    var buttons = Field<GameObject>(UIManager_Menu.Instance, "Panel_Option").GetComponentsInChildren<Button>(true).Where(b => Themes.Contains(b.name)).ToArray();
                    Check(buttons.Length == 8 && buttons.All(b => b.image.sprite == Theme(Array.IndexOf(Themes, b.name)).BarSprite), "Original map bars on options buttons");
                    CheckArt(); Capture("direct-options");
                    SceneManager.LoadScene("3. GameScene"); Next(4, 8f); break;
                case 4:
                    Check(SlimeManager.Instance.GetComponentsInChildren<SlimeController>().Length == 170, "170 slimes");
                    Time.timeScale = 0f;
                    int digit = 0;
                    foreach (var slime in SlimeManager.Instance.GetComponentsInChildren<SlimeController>()) slime.SetNumber(digit++ % 9 + 1);
                    SessionState.SetInt("Direct.Index", 0); Map(UIManager_Game.Instance, Theme(0)); Next(5); break;
                case 5:
                    var images = UnityEngine.Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
                    Check(images.First(n => n.name == "GameTable").sprite == Theme(i).TableSprite, "Live original table");
                    Check(images.First(n => n.name == "DoodleThemeTimerEdge").sprite == Theme(i).BarSprite, "Live original timer bar");
                    Check(images.First(n => n.name == "DoodleThemeMascot").sprite == Theme(i).SlimeSprite, "Live original mascot");
                    Check(images.Count(n => n.enabled && n.sprite == Theme(i).SlimeSprite) == 1, "Only header mascot enabled");
                    foreach (var slime in SlimeManager.Instance.GetComponentsInChildren<SlimeController>())
                        Check(AssetDatabase.GetAssetPath(slime.transform.Find("Color").GetComponent<SpriteRenderer>().sprite) == "Assets/4. SlimeAsset/Slime/Slime.png", "Original slime body");
                    CheckArt(); Capture("direct-game-" + Themes[i]);
                    if (++i < 8) { SessionState.SetInt("Direct.Index", i); Map(UIManager_Game.Instance, Theme(i)); Next(5); }
                    else { Field<Button>(UIManager_Game.Instance, "btn_menu").onClick.Invoke(); Next(6); }
                    break;
                case 6:
                    Check(Field<GameObject>(UIManager_Game.Instance, "Panel_Main").GetComponent<Image>().sprite == Theme(7).MenuSprite, "Pause uses original paper");
                    CheckArt(); Capture("direct-pause");
                    Field<Button>(UIManager_Game.Instance, "btn_Option").onClick.Invoke(); Next(7); break;
                case 7:
                    Field<Button>(UIManager_Game.Instance, "btn_slimeNormal").onClick.Invoke();
                    Field<Button>(UIManager_Game.Instance, "btn_slimeKing").onClick.Invoke();
                    Check(AssetDatabase.GetAssetPath(ChangeData.SelectedFace) == "Assets/4. SlimeAsset/Slime/Face_1.png", "Original selectable face");
                    Check(AssetDatabase.GetAssetPath(ChangeData.SelectedCostume) == "Assets/4. SlimeAsset/Slime/Costume_King.png", "Original selectable crown");
                    CheckArt(); Capture("direct-game-options");
                    Field<Button>(UIManager_Game.Instance, "btn_quit").onClick.Invoke();
                    Field<Button>(UIManager_Game.Instance, "btn_Resume").onClick.Invoke();
                    Check(!GameManager.Instance.IsPaused && SlimeManager.Instance.GetComponentsInChildren<SlimeController>().Length == 170, "Resume keeps board");
                    Capture("direct-game-costume");
                    File.WriteAllText(Path.Combine(Output, "Validation.txt"), "PASS: 66 serialized skin bindings; no DoodleLab Resources sprites; game skin UI and SpriteRenderers use SlimeAsset paths (color-picker internals and Unity primitives retained); original menu/table/bar/mascot references in all 8 themes; original game bodies, face and costume selections; all 3 scene entry points; menu/options/game/pause/resume; 170 slimes; no runtime errors.\n");
                    SessionState.SetBool(Key, false); EditorApplication.Exit(0); break;
            }
        } catch (Exception e) {
            File.WriteAllText(Path.Combine(Output, "Validation.txt"), "FAIL phase " + SessionState.GetInt("Direct.Phase", 0) + ": " + e);
            SessionState.SetBool(Key, false); Debug.LogException(e); EditorApplication.Exit(1);
        }
    }
}
