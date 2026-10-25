$ErrorActionPreference='Stop'
$stage=$PSScriptRoot
$project='C:\Users\user\Desktop\Programing\Project-B-SilmePOP'
$path='Assets/3. Scripts/DoodleLabSkin.cs'
$text=[IO.File]::ReadAllText((Join-Path $project $path))
$old=@'
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
'@
$new=@'
    public static void Register(DoodleSkinAssets assets)
    {
        sprites = new Dictionary<string, Sprite>();
        if (assets == null || assets.sprites == null) return;
        foreach (var binding in assets.sprites)
            if (!string.IsNullOrEmpty(binding.name) && binding.sprite != null)
                sprites[binding.name] = binding.sprite;
    }

    public static Sprite Sprite(string name) =>
        sprites != null && sprites.TryGetValue(name, out var value) ? value : null;

    private static bool IsMapMascot(Sprite sprite)
    {
        if (sprite == null) return false;
        if (sprite.name == "Mascot") return true;
        if (sprites == null) return false;
        foreach (var binding in sprites)
            if (binding.Key.EndsWith("_Mascot") && binding.Value == sprite) return true;
        return false;
    }

    private static void FitMascot(Image image, float height)
    {
        if (image == null || image.sprite == null) return;
        image.rectTransform.sizeDelta = new Vector2(height * image.sprite.rect.width / image.sprite.rect.height, height);
        image.preserveAspect = true;
    }

    private static void FitLogo(Image image, float width, float height)
    {
        // Source PNGs include transparent margins; retain the intended visible logo size.
        if (image == null || image.sprite == null) return;
        var vertices = image.sprite.vertices;
        if (vertices.Length == 0) return;
        var min = vertices[0];
        var max = vertices[0];
        foreach (var vertex in vertices) { min = Vector2.Min(min, vertex); max = Vector2.Max(max, vertex); }
        var size = max - min;
        if (size.x <= 0f || size.y <= 0f) return;
        float scale = Mathf.Min(width / size.x, height / size.y);
        image.rectTransform.sizeDelta = image.sprite.rect.size / image.sprite.pixelsPerUnit * scale;
        image.rectTransform.anchoredPosition -= (min + max) * (0.5f * scale);
        image.preserveAspect = true;
    }
'@
$text=$text.Replace("`r`n","`n")
if(-not $text.Contains($old.Replace("`r`n","`n"))){throw 'Skin loader changed'}
$text=$text.Replace($old.Replace("`r`n","`n"),$new.Replace("`r`n","`n"))
$text=$text.Replace(@'
        if (image == null) return;
        var sprite = Sprite(spriteName);
        if (sprite == null) return;
'@.Replace("`r`n","`n"),@'
        SetThemeImage(image, Sprite(spriteName), scale, spriteName.EndsWith("_Bar") ? 128f : 512f);
    }

    private static void SetThemeImage(Image image, Sprite sprite, float scale, float referenceHeight = 512f)
    {
        if (image == null || sprite == null) return;
'@.Replace("`r`n","`n"))
$text=$text.Replace('image.pixelsPerUnitMultiplier = scale;','image.pixelsPerUnitMultiplier = scale * sprite.rect.height / referenceHeight;')
$text=$text.Replace('SetThemeImage(Find<Image>(scene, "GameTable"), prefix + "_Table", 3.5f);','SetThemeImage(Find<Image>(scene, "GameTable"), data != null ? data.TableSprite : Sprite(prefix + "_Table"), 3.5f);')
$text=$text.Replace('SetThemeImage(Find<Image>(scene, "OptionPanel"), prefix + "_Menu", 2f);','SetThemeImage(Find<Image>(scene, "OptionPanel"), data != null ? data.OptionPanelSprite : Sprite(prefix + "_Menu"), 2f);')
$text=$text.Replace('image.sprite = Sprite(prefix + "_Mascot");','image.sprite = data != null ? data.SlimeSprite : Sprite(prefix + "_Mascot");')
$text=$text.Replace("                    image.preserveAspect = true;`n                }`n        var track", "                    FitMascot(image, 190f);`n                }`n        var track")
$text=$text.Replace('SetThemeImage(edge, prefix + "_Bar", 2f);','SetThemeImage(edge, data != null ? data.BarSprite : Sprite(prefix + "_Bar"), 2f, 128f);')
$text=$text.Replace('mascot.sprite = Sprite(prefix + "_Mascot");','mascot.sprite = data != null ? data.SlimeSprite : Sprite(prefix + "_Mascot");')
$text=$text.Replace('Place(mascot.rectTransform, -382, 425, 82, 82);',"Place(mascot.rectTransform, -382, 425, 82, 82);`n            FitMascot(mascot, 82f);")
$text=$text.Replace('image.sprite != null && image.sprite.name == "Logo"','image.sprite != null && image.sprite == Sprite("Logo")')
$text=$text.Replace('Place(image.rectTransform, 0, 170, 1300, 440); image.preserveAspect = true;', 'Place(image.rectTransform, 0, 170, 1300, 440); FitLogo(image, 1300, 440);')
$text=$text.Replace('Place(logo.rectTransform, -650, 425, 445, 150);', 'Place(logo.rectTransform, -650, 425, 445, 150); FitLogo(logo, 445, 150);')
$text=$text.Replace('Place(logo.rectTransform, -5, 345, 600, 200);', 'Place(logo.rectTransform, -5, 345, 600, 200); FitLogo(logo, 600, 200);')
$text=$text.Replace('(image.sprite.name == "Mascot" || image.sprite.name.EndsWith("_Mascot"))','IsMapMascot(image.sprite)')
$text=$text.Replace('Place(handle.rectTransform, 0, 0, 60, 60);','Place(handle.rectTransform, 0, 0, 120, 60);')
$text=$text.Replace('Place(face.rectTransform, 0, 0, 60, 60);','Place(face.rectTransform, 0, 0, 120, 60);')
$text=$text.Replace('handle.rectTransform.sizeDelta = new Vector2(60, 0);','handle.rectTransform.sizeDelta = new Vector2(120, 0);')
$text=$text.Replace('Place(image.rectTransform, 0, 15, 130, 130);','Place(image.rectTransform, 0, 15, image.sprite != null ? 130f * image.sprite.rect.width / image.sprite.rect.height : 130f, 130);')
if($text.Contains('Resources.')){throw 'Resource loading remains'}
$dest=Join-Path $stage ('payload/'+$path);[IO.Directory]::CreateDirectory((Split-Path $dest)) | Out-Null
[IO.File]::WriteAllText($dest,$text,[Text.UTF8Encoding]::new($false))
$path='Assets/3. Scripts/ThemeData.cs';$bytes=[IO.File]::ReadAllBytes((Join-Path $project $path));$encoding=[Text.UTF8Encoding]::new($false,$true)
try{$text=$encoding.GetString($bytes)}catch{$encoding=[Text.Encoding]::GetEncoding(949);$text=$encoding.GetString($bytes)}
$index=$text.LastIndexOf('}')
$text=$text.Insert($index,"    public Sprite BarSprite;`n    public Sprite OptionPanelSprite;`n")
[IO.File]::WriteAllText((Join-Path $stage ('payload/'+$path)),$text,$encoding)
Write-Output 'Removed runtime Resources skin loading; wired ThemeData originals and preserved image sizing.'
