using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Presentation for the notebook skin. Game rules and input stay with the existing managers.</summary>
public static class DoodleLabSkin
{
    public static readonly Color Ink = new Color(0.16f, 0.19f, 0.18f);
    private static readonly Color[] Palette =
    {
        new Color(0.72f, 0.95f, 0.34f), new Color(0.76f, 0.97f, 0.39f),
        new Color(0.69f, 0.92f, 0.31f), new Color(0.79f, 0.97f, 0.43f)
    };
    private static Dictionary<string, Sprite> sprites;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        sprites = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() => SceneManager.sceneLoaded += OnSceneLoaded;

    public static Sprite Sprite(string name)
    {
        if (sprites == null)
        {
            sprites = new Dictionary<string, Sprite>();
            foreach (var sprite in Resources.LoadAll<Sprite>("DoodleLab"))
                sprites[sprite.name] = sprite;
        }
        return sprites.TryGetValue(name, out var value) ? value : null;
    }

    public static Color SlimeColor(Color selected, int number)
    {
        if (!ChangeData.UseDoodlePalette) return selected;
        return Palette[Mathf.Max(0, number - 1) % Palette.Length];
    }

    public static Color ThemeColor(string theme)
    {
        switch (theme)
        {
            case "Spring": return new Color(1f, 0.88f, 0.91f);
            case "Summer": return new Color(0.84f, 0.97f, 0.91f);
            case "Autumn": return new Color(1f, 0.88f, 0.72f);
            case "Winter": return new Color(0.88f, 0.94f, 1f);
            case "Sunny": return new Color(1f, 0.97f, 0.75f);
            case "Night": return new Color(0.80f, 0.83f, 0.96f);
            case "Cloudy": return new Color(0.87f, 0.91f, 0.94f);
            default: return Color.white;
        }
    }

    public static void ApplySlime(SlimeController slime)
    {
        var body = slime.transform.Find("Color");
        Color color = SlimeColor(ChangeData.SelectedColor, slime.CurrentNumber);
        if (body != null && body.TryGetComponent<SpriteRenderer>(out var renderer))
            renderer.color = color;
        slime.SetSlimeColor(color);
    }

    public static void StyleSlimeNumber(TMP_Text number)
    {
        number.fontStyle = FontStyles.Bold;
        number.fontWeight = FontWeight.Bold;
        number.enableAutoSizing = false;
        number.fontSize = 0.48f;
        number.alignment = TextAlignmentOptions.Center;
        number.margin = Vector4.zero;
        number.raycastTarget = false;
        number.rectTransform.anchoredPosition = new Vector2(0f, -0.04f);
    }
    private static void SetThemeImage(Image image, string spriteName, float scale)
    {
        if (image == null) return;
        var sprite = Sprite(spriteName);
        if (sprite == null) return;
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = scale;
        image.preserveAspect = false;
    }

    public static void ApplyMapTheme(ThemeData data)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.path.StartsWith("Assets/1. Scenes/")) return;
        string theme = data != null ? data.themeName : "Basic";
        string prefix = "Map_" + theme;
        SetThemeImage(Find<Image>(scene, "GameTable"), prefix + "_Table", 3.5f);
        var menuPaper = Find<Image>(scene, "MenuPanel");
        if (menuPaper != null && data != null && data.MenuSprite != null)
        {
            menuPaper.sprite = data.MenuSprite;
            menuPaper.color = Color.white;
            menuPaper.type = Image.Type.Simple;
            menuPaper.preserveAspect = false;
        }
        var pausePaper = Find<Image>(scene, "MainMenuPanel");
        if (pausePaper != null && data != null && data.MenuSprite != null)
        {
            pausePaper.sprite = data.MenuSprite;
            pausePaper.color = Color.white;
            pausePaper.type = Image.Type.Simple;
            pausePaper.preserveAspect = false;
            foreach (var button in pausePaper.GetComponentsInChildren<Button>(true))
                foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    Place(text.rectTransform, 0, 0, 410, 80);
                    text.fontSize = 44;
                    text.alignment = TextAlignmentOptions.Center;
                    text.color = Ink;
                    text.outlineWidth = 0f;
                }
        }
        SetThemeImage(Find<Image>(scene, "OptionPanel"), prefix + "_Menu", 2f);
        var menu = Find<RectTransform>(scene, "MenuPanel");
        if (menu != null)
            foreach (var image in menu.GetComponentsInChildren<Image>(true))
                if (image.name == "Slime_Image")
                {
                    image.sprite = Sprite(prefix + "_Mascot");
                    image.color = Color.white;
                    image.preserveAspect = true;
                }
        var track = Find<Image>(scene, "DoodleTimerTrack");
        if (track != null)
        {
            var existing = track.transform.Find("DoodleThemeTimerEdge");
            var edge = existing != null ? existing.GetComponent<Image>() : AddImage(track.transform, "DoodleThemeTimerEdge", prefix + "_Bar");
            SetThemeImage(edge, prefix + "_Bar", 2f);
            edge.fillCenter = false;
            edge.raycastTarget = false;
            edge.rectTransform.anchorMin = Vector2.zero;
            edge.rectTransform.anchorMax = Vector2.one;
            edge.rectTransform.offsetMin = edge.rectTransform.offsetMax = Vector2.zero;
            edge.transform.SetAsLastSibling();
        }
        var game = Find<RectTransform>(scene, "GamePanel");
        if (game != null)
        {
            var existing = game.Find("DoodleThemeMascot");
            var mascot = existing != null ? existing.GetComponent<Image>() : AddImage(game, "DoodleThemeMascot", prefix + "_Mascot");
            mascot.sprite = Sprite(prefix + "_Mascot");
            mascot.color = Color.white;
            mascot.preserveAspect = true;
            mascot.raycastTarget = false;
            Place(mascot.rectTransform, -382, 425, 82, 82);
        }
        FitBackground(scene);
    }
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ignore imported example scenes and any other projects' content.
        if (!scene.path.StartsWith("Assets/1. Scenes/")) return;
        ApplyScene(scene);
    }

    public static void ApplyScene(Scene scene)
    {
        TMP_FontAsset font = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.color = Ink;
                text.outlineWidth = 0f;
                text.raycastTarget = false;
                if (font == null && text.font != null) font = text.font;
            }
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                // The old full-screen hollow border cannot be replaced with an opaque sticker.
                // The new parent panels already supply the notebook border.
                if (image.name == "Frame" && image.sprite != null && image.sprite.name.StartsWith("Frame_"))
                {
                    image.enabled = false;
                    image.raycastTarget = false;
                    continue;
                }
                if (image.sprite != null && image.sprite.name.StartsWith("Frame_"))
                {
                    image.type = Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 2f;
                    image.preserveAspect = false;
                }
                else if (image.sprite != null && image.sprite.texture.name == "Sprites")
                    image.preserveAspect = true;
                // These used to be hollow decorative borders drawn in front of the option controls.
                if (image.name == "BackGround" && image.sprite != null && image.sprite.name.StartsWith("Frame_"))
                {
                    image.transform.SetAsFirstSibling();
                    image.fillCenter = false;
                    image.raycastTarget = false;
                }
            }
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.98f, 0.88f);
                colors.pressedColor = new Color(0.82f, 0.89f, 0.84f);
                colors.disabledColor = new Color(0.66f, 0.66f, 0.66f, 0.55f);
                colors.fadeDuration = 0.09f;
                button.colors = colors;
            }
        }
        if (font == null) font = TMP_Settings.defaultFontAsset;

        Canvas canvas = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var item in root.GetComponentsInChildren<Canvas>(true))
                if (item.isRootCanvas && item.renderMode != RenderMode.WorldSpace) { canvas = item; break; }
        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }
        FitBackground(scene);
        StyleOptions(scene, font);

        if (scene.name.Contains("GameScene")) StyleGame(scene, font);
        else if (scene.name.Contains("GameStart")) StyleStart(scene, font);
        else StyleMenu(scene, font);
        ApplyMapTheme(ChangeData.SelectedMapTheme);
    }

    private static void FitBackground(Scene scene)
    {
        var background = Find<SpriteRenderer>(scene, "BackGround");
        var camera = Find<Camera>(scene, "Main Camera");
        if (background == null || camera == null || background.sprite == null) return;
        float height = camera.orthographicSize * 2f;
        var bounds = background.sprite.bounds.size;
        background.transform.localScale = new Vector3(height * camera.aspect / bounds.x, height / bounds.y, 1f);
        background.color = ThemeColor(ChangeData.SelectedMapTheme != null ? ChangeData.SelectedMapTheme.themeName : "Basic");
    }

    private static void StyleGame(Scene scene, TMP_FontAsset font)
    {
        var panel = Find<RectTransform>(scene, "GamePanel");
        if (panel == null) return;
        var panelImage = panel.GetComponent<Image>();
        if (panelImage != null) { panelImage.sprite = null; panelImage.color = Color.clear; }

        var table = Find<RectTransform>(scene, "GameTable");
        Place(table, 0, 0, 1650, 690);
        if (table != null && table.TryGetComponent<Image>(out var tableImage))
        {
            SetFrame(tableImage, "Frame_Cream");
            tableImage.raycastTarget = false;
            if (table.Find("DoodleGrid") == null)
            {
                var gridObject = new GameObject("DoodleGrid", typeof(RectTransform), typeof(DoodleLabGrid));
                gridObject.transform.SetParent(table, false);
                var grid = gridObject.GetComponent<DoodleLabGrid>();
                Place(grid.rectTransform, 0, 0, 1549.125f, 600f);
                grid.color = new Color(0.37f, 0.46f, 0.47f, 0.23f);
                grid.raycastTarget = false;
            }
        }

        if (panel.Find("DoodleLogo") == null)
        {
            var logo = AddImage(panel, "DoodleLogo", "Logo");
            Place(logo.rectTransform, -650, 425, 445, 150);
            logo.preserveAspect = true;
        }

        var timerText = Find<TMP_Text>(scene, "Txt_Time");
        if (timerText != null)
        {
            var slider = timerText.GetComponentInParent<Slider>();
            if (slider != null)
            {
                Place(slider.GetComponent<RectTransform>(), -40, 425, 610, 80);
                if (slider.fillRect != null && slider.fillRect.TryGetComponent<Image>(out var fill))
                { fill.sprite = Sprite("Frame_Mint"); fill.color = Color.white; }
            }
            Place(timerText.rectTransform, 0, 0, 500, 60);
            timerText.fontSize = 36;
            timerText.alignment = TextAlignmentOptions.Center;
        }

        foreach (var score in panel.GetComponentsInChildren<Image>(true))
        {
            if (score.name != "Score" || score.transform.parent != panel) continue;
            Place(score.rectTransform, 605, 425, 330, 100);
            SetFrame(score, "Frame_Tape");
            var value = score.GetComponentInChildren<TMP_Text>(true);
            if (value != null)
            {
                Place(value.rectTransform, 65, 0, 130, 65);
                value.fontSize = 45;
            }
            AddLabel(score.transform, "DoodleScoreLabel", "SCORE", font, 25, -65, 0, 130, 50);
        }

        var pause = Find<Button>(scene, "Menu");
        if (pause != null)
        {
            Place(pause.GetComponent<RectTransform>(), 825, 425, 90, 90);
            pause.image.sprite = Sprite("Pause");
            pause.image.type = Image.Type.Simple;
            pause.image.preserveAspect = true;
        }

        StyleSkill(scene, "Slime_Magnifier", "Magnifier", "Frame_Yellow", "HINT  [A]", -400, font);
        StyleSkill(scene, "Slime_Mix", "Mix", "Frame_Blue", "MIX  [S]", 0, font);
        StyleSkill(scene, "Slime_Destroy", "Eraser", "Frame_Coral", "POP  [D]", 400, font);

        var warning = Find<TMP_Text>(scene, "Txt_Warning");
        if (warning != null)
        {
            Place(warning.rectTransform, 0, 60, 1500, 180);
            warning.fontSize = 42;
        }
        AddLabel(panel, "DoodleInstructions", "DRAG A RECTANGLE  /  MAKE 10!", font, 22, 0, -505, 1050, 38);
    }

    private static void StyleSkill(Scene scene, string name, string icon, string frame, string title, float x, TMP_FontAsset font)
    {
        var button = Find<Button>(scene, name);
        if (button == null) return;
        foreach (var change in button.GetComponentsInChildren<Change>(true)) change.enabled = false;
        Place(button.GetComponent<RectTransform>(), x, -410, 350, 135);
        SetFrame(button.image, frame);
        foreach (var image in button.GetComponentsInChildren<Image>(true))
        {
            if (image == button.image) continue;
            image.sprite = Sprite(icon);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Place(image.rectTransform, -105, 0, 90, 90);
        }
        foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "txt_A" || text.name == "txt_S" || text.name == "txt_D")
            { text.gameObject.SetActive(false); continue; }
            Place(text.rectTransform, 40, -27, 170, 40);
            text.fontSize = 26;
            text.alignment = TextAlignmentOptions.Center;
        }
        AddLabel(button.transform, "DoodleSkillTitle", title, font, 27, 40, 24, 195, 42);
    }

    private static void StyleStart(Scene scene, TMP_FontAsset font)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                if (image.sprite != null && image.sprite.name == "Logo")
                { Place(image.rectTransform, 0, 170, 1300, 440); image.preserveAspect = true; }
        var start = Find<Button>(scene, "Btn_GameStart");
        if (start == null) return;
        Place(start.GetComponent<RectTransform>(), 0, -210, 650, 180);
        SetFrame(start.image, "Frame_Mint");
        AddLabel(start.transform, "DoodleStartLabel", "GAME START", font, 62, 0, 0, 570, 100);
        AddLabel(start.transform.parent, "DoodleSubtitle", "A LITTLE SLIME EXPERIMENT", font, 26, 0, -380, 1000, 50);
    }

    private static void StyleMenu(Scene scene, TMP_FontAsset font)
    {
        // Existing option/menu hierarchy and button bindings are kept intact.
        var menu = Find<RectTransform>(scene, "MenuPanel");
        if (menu == null) return;
        var image = menu.GetComponent<Image>();
        if (image != null) image.raycastTarget = false;
        if (menu.Find("DoodleMenuLogo") == null)
        {
            var logo = AddImage(menu, "DoodleMenuLogo", "Logo");
            Place(logo.rectTransform, -5, 345, 600, 200);
            logo.preserveAspect = true;
        }
        string[] buttonNames = { "Btn_GameStart", "Btn_Option", "Btn_Help", "Btn_Quit" };
        for (int i = 0; i < buttonNames.Length; i++)
        {
            var button = menu.GetComponentsInChildren<Button>(true);
            foreach (var item in button)
            {
                if (item.name != buttonNames[i]) continue;
                Place(item.GetComponent<RectTransform>(), 0, 155 - i * 150, 550, 115);
                SetFrame(item.image, i == 0 ? "Frame_Mint" : "Frame_Cream");
                foreach (var text in item.GetComponentsInChildren<TMP_Text>(true))
                {
                    Place(text.rectTransform, 0, 0, 490, 85);
                    text.fontSize = 45;
                    text.alignment = TextAlignmentOptions.Center;
                }
            }
        }
        foreach (var mascot in menu.GetComponentsInChildren<Image>(true))
            if (mascot.name == "Slime_Image")
            { Place(mascot.rectTransform, 405, -350, 190, 190); mascot.preserveAspect = true; }
    }

    private static void SetFrame(Image image, string name)
    {
        if (image == null) return;
        image.sprite = Sprite(name);
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3f;
        image.color = Color.white;
        image.preserveAspect = false;
    }

    public static void ConfigureHeader(GameObject gamePanel, Slider timer, TMP_Text timeText, TMP_Text scoreText)
    {
        if (gamePanel == null) return;
        var panel = gamePanel.transform;
        // The original HUD mixed world-space canvases with screen-space controls.
        // Move the existing bound texts into the main canvas so their update events remain intact.
        if (timer != null)
        {
            timer.transform.SetParent(panel, false);
            foreach (var nestedCanvas in timer.GetComponentsInChildren<Canvas>(true)) Object.Destroy(nestedCanvas);
            foreach (var change in timer.GetComponentsInChildren<Change>(true)) change.enabled = false;
            foreach (var image in timer.GetComponentsInChildren<Image>(true)) image.enabled = false;
            Place(timer.GetComponent<RectTransform>(), -30, 425, 600, 85);
            var track = AddImage(timer.transform, "DoodleTimerTrack", "Frame_Outline");
            SetFrame(track, "Frame_Outline");
            Place(track.rectTransform, 0, 0, 600, 85);
            var fill = AddImage(track.transform, "DoodleTimerFill", "Frame_Mint");
            SetFrame(fill, "Frame_Mint");
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(7, 7);
            fill.rectTransform.offsetMax = new Vector2(-7, -7);
            timer.direction = Slider.Direction.LeftToRight;
            timer.fillRect = fill.rectTransform;
            timer.handleRect = null;
            timer.targetGraphic = track;
            timer.transition = Selectable.Transition.None;
            timer.interactable = false;
            if (timeText != null)
            {
                timeText.transform.SetParent(timer.transform, false);
                timeText.transform.SetAsLastSibling();
                Place(timeText.rectTransform, 0, 0, 460, 55);
                timeText.fontSize = 34;
                timeText.margin = Vector4.zero;
                timeText.color = Ink;
                timeText.outlineWidth = 0f;
                timeText.alignment = TextAlignmentOptions.Center;
            }
        }
        if (scoreText != null)
        {
            var previousCanvas = scoreText.canvas;
            var previousParent = scoreText.transform.parent;
            var frame = AddImage(panel, "DoodleScore", "Frame_Tape");
            SetFrame(frame, "Frame_Tape");
            Place(frame.rectTransform, 605, 425, 340, 110);
            scoreText.transform.SetParent(frame.transform, false);
            Place(scoreText.rectTransform, 80, 0, 115, 65);
            scoreText.fontSize = 44;
            scoreText.color = Ink;
            scoreText.outlineWidth = 0f;
            scoreText.alignment = TextAlignmentOptions.Center;
            if (previousCanvas != null && previousCanvas.renderMode == RenderMode.WorldSpace)
                previousCanvas.gameObject.SetActive(false);
            if (previousParent != null && previousParent != panel && previousParent.TryGetComponent<Image>(out var oldBacking))
                oldBacking.enabled = false;
            AddLabel(frame.transform, "DoodleScoreCaption", "SCORE", scoreText.font, 26, -65, 0, 135, 55);
        }
        foreach (var image in gamePanel.GetComponentsInChildren<Image>(true))
            if (image.name != "DoodleThemeMascot" && image.sprite != null && (image.sprite.name == "Mascot" || image.sprite.name.EndsWith("_Mascot"))) image.enabled = false;
        ApplyMapTheme(ChangeData.SelectedMapTheme);
    }

    private static void StyleOptions(Scene scene, TMP_FontAsset font)
    {
        var panel = Find<RectTransform>(scene, "OptionPanel");
        if (panel == null) return;
        string[] channels = { "Slider_Master", "Slider_BGM", "Slider_SFX" };
        string[] captions = { "Master", "BGM", "SFX" };
        for (int i = 0; i < channels.Length; i++)
        {
            var slider = Find<Slider>(scene, channels[i]);
            if (slider == null) continue;
            slider.transform.SetParent(panel, false);
            Place(slider.GetComponent<RectTransform>(), -500, 235 - i * 110, 330, 60);
            foreach (var image in slider.GetComponentsInChildren<Image>(true)) image.enabled = false;
            foreach (var text in slider.GetComponentsInChildren<TMP_Text>(true)) text.gameObject.SetActive(false);
            var track = AddImage(slider.transform, "DoodleVolumeTrack", "Frame_Outline");
            SetFrame(track, "Frame_Outline");
            track.raycastTarget = true;
            Place(track.rectTransform, 0, 0, 330, 60);
            var fill = AddImage(track.transform, "DoodleVolumeFill", "Frame_Mint");
            SetFrame(fill, "Frame_Mint");
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(4, 4);
            fill.rectTransform.offsetMax = new Vector2(-4, -4);
            var handle = AddImage(slider.transform, "DoodleVolumeHandle", "Body");
            handle.raycastTarget = true;
            handle.color = Palette[0];
            Place(handle.rectTransform, 0, 0, 60, 60);
            var face = AddImage(handle.transform, "DoodleHandleFace", "Face");
            Place(face.rectTransform, 0, 0, 60, 60);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            slider.targetGraphic = handle;
            handle.rectTransform.sizeDelta = new Vector2(60, 0);
            AddLabel(panel, "DoodleVolumeCaption" + i, captions[i], font, 26, -500, 188 - i * 110, 330, 45);
        }
        string[] themes = { "Basic", "Spring", "Summer", "Autumn", "Winter", "Sunny", "Night", "Cloudy" };
        for (int i = 0; i < themes.Length; i++)
        {
            var button = Find<Button>(scene, themes[i]);
            if (button == null) continue;
            button.transform.SetParent(panel, false);
            Place(button.GetComponent<RectTransform>(), 500, 220 - i * 74, 340, 65);
            foreach (var change in button.GetComponentsInChildren<Change>(true)) change.enabled = false;
            SetFrame(button.image, "Frame_Cream");
            SetThemeImage(button.image, "Map_" + themes[i] + "_Bar", 2f);
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                Place(text.rectTransform, 0, 0, 290, 45);
                text.fontSize = 25;
                text.alignment = TextAlignmentOptions.Center;
            }
        }
        foreach (var button in panel.GetComponentsInChildren<Button>(true))
        {
            string group = button.transform.parent.name;
            if (group != "Face" && group != "Costume") continue;
            SetFrame(button.image, "Frame_Cream");
            foreach (var image in button.GetComponentsInChildren<Image>(true))
            {
                if (image == button.image) continue;
                Place(image.rectTransform, 0, 15, 130, 130);
                image.preserveAspect = true;
            }
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                Place(text.rectTransform, 0, -55, 145, 35);
                text.fontSize = 24;
            }
        }
        foreach (var image in panel.GetComponentsInChildren<Image>(true))
            if (image.name == "Slime_Image") image.enabled = false;
    }

    private static Image AddImage(Transform parent, string name, string sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = Sprite(sprite);
        image.raycastTarget = false;
        return image;
    }

    private static void AddLabel(Transform parent, string name, string value, TMP_FontAsset font, float size, float x, float y, float width, float height)
    {
        if (parent.Find(name) != null) return;
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = Ink;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        Place(text.rectTransform, x, y, width, height);
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static T Find<T>(Scene scene, string name) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var item in root.GetComponentsInChildren<T>(true))
                if (item.name == name) return item;
        return null;
    }
}

/// <summary>Lightweight notebook ruling, drawn underneath the existing world-space slimes.</summary>
public sealed class DoodleLabGrid : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        Rect rect = rectTransform.rect;
        for (int x = 0; x <= 17; x++)
        {
            float px = rect.xMin + rect.width * x / 17f;
            Quad(helper, px - 0.6f, rect.yMin, px + 0.6f, rect.yMax);
        }
        for (int y = 0; y <= 10; y++)
        {
            float py = rect.yMin + rect.height * y / 10f;
            Quad(helper, rect.xMin, py - 0.6f, rect.xMax, py + 0.6f);
        }
    }

    private void Quad(VertexHelper helper, float left, float bottom, float right, float top)
    {
        int start = helper.currentVertCount;
        helper.AddVert(new Vector3(left, bottom), color, Vector2.zero);
        helper.AddVert(new Vector3(left, top), color, Vector2.zero);
        helper.AddVert(new Vector3(right, top), color, Vector2.zero);
        helper.AddVert(new Vector3(right, bottom), color, Vector2.zero);
        helper.AddTriangle(start, start + 1, start + 2);
        helper.AddTriangle(start, start + 2, start + 3);
    }
}
