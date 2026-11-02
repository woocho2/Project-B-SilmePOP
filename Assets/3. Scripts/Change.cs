using UnityEngine;
using UnityEngine.UI;

public class Change : MonoBehaviour
{
    public enum Category
    {
        Slime,
        Map
    }

    public enum SlimePart
    {
        Color,
        Face,
        Costume,
        SliderBar
    }

    public enum MapPart
    {
        Menu,
        GameTable,
        Slime
    }

    [SerializeField] private Category category = Category.Slime;
    [SerializeField] private SlimePart slimepart;
    [SerializeField] private MapPart mappart;

    private bool started;
    private UIManager_Menu menuPublisher;
    private UIManager_Game gamePublisher;
    private Image targetimg;
    private SpriteRenderer targetsr;
    private SlimeController m_slimeController;

    private void Awake()
    {
        targetimg = GetComponent<Image>();
        targetsr = GetComponent<SpriteRenderer>();
        m_slimeController = GetComponentInParent<SlimeController>();
    }

    private void Start()
    {
        started = true;
        ManageEvents(true);
        ApplySavedData();
    }

    private void OnEnable()
    {
        if (!started) return;
        ManageEvents(true);
        ApplySavedData();
    }

    private void OnDisable()
    {
        ManageEvents(false);
    }

    private void ManageEvents(bool subscribe)
    {
        if (subscribe)
        {
            menuPublisher = UIManager_Menu.Instance;
            gamePublisher = UIManager_Game.Instance;
        }
        if (menuPublisher != null)
        {
            if (subscribe)
            {
                if (category == Category.Slime && (slimepart == SlimePart.Color || slimepart == SlimePart.SliderBar)) menuPublisher.OnColorChanged += ApplyColor;
                if (category == Category.Slime && slimepart == SlimePart.Face) menuPublisher.OnFaceChanged += ApplyFace;
                if (category == Category.Slime && slimepart == SlimePart.Costume) menuPublisher.OnCostumeChanged += ApplyCostume;
                if (category == Category.Map) menuPublisher.OnMapChanged += ApplyMap;
            }
            else
            {
                menuPublisher.OnColorChanged -= ApplyColor;
                menuPublisher.OnFaceChanged -= ApplyFace;
                menuPublisher.OnCostumeChanged -= ApplyCostume;
                menuPublisher.OnMapChanged -= ApplyMap;
            }
        }

        if (gamePublisher != null)
        {
            if (subscribe)
            {
                if (category == Category.Slime && (slimepart == SlimePart.Color || slimepart == SlimePart.SliderBar)) gamePublisher.OnColorChanged += ApplyColor;
                if (category == Category.Slime && slimepart == SlimePart.Face) gamePublisher.OnFaceChanged += ApplyFace;
                if (category == Category.Slime && slimepart == SlimePart.Costume) gamePublisher.OnCostumeChanged += ApplyCostume;
                if (category == Category.Map) gamePublisher.OnMapChanged += ApplyMap;
            }
            else
            {
                gamePublisher.OnColorChanged -= ApplyColor;
                gamePublisher.OnFaceChanged -= ApplyFace;
                gamePublisher.OnCostumeChanged -= ApplyCostume;
                gamePublisher.OnMapChanged -= ApplyMap;
            }
        }
    }

    public void ApplySavedData()
    {
        if (category == Category.Slime)
        {
            if (slimepart == SlimePart.Color || slimepart == SlimePart.SliderBar)
            {
                ApplyColor(ChangeData.SelectedColor);
            }
            else if (slimepart == SlimePart.Face)
            {
                ApplyFace(ChangeData.SelectedFace);
            }
            else if (slimepart == SlimePart.Costume)
            {
                ApplyCostume(ChangeData.SelectedCostume);
            }
        }
        else if (category == Category.Map)
        {
            if (ChangeData.SelectedMapTheme != null)
            {
                ApplyMap(ChangeData.SelectedMapTheme);
            }
        }
    }

    public void ApplyColor(Color color)
    {
        if (category == Category.Slime)
        {
            if (slimepart == SlimePart.Color)
            {
                if (m_slimeController != null)
                {
                    DoodleLabSkin.ApplySlime(m_slimeController);
                    return;
                }

                Sprite bodySprite = DoodleLabSkin.Sprite("Body");
                if (bodySprite != null)
                {
                    if (targetimg != null) { targetimg.sprite = bodySprite; targetimg.preserveAspect = true; }
                    if (targetsr != null) targetsr.sprite = bodySprite;
                }
                color = DoodleLabSkin.SlimeColor(color, 0);
                if (targetimg != null) targetimg.color = color;
                if (targetsr != null) targetsr.color = color;
            }
            else if (slimepart == SlimePart.SliderBar)
            {
                if (targetimg != null) targetimg.color = color;
                if (targetsr != null) targetsr.color = color;
            }
        }
    }

    public void ApplyFace(Sprite faceSprite)
    {
        if (category != Category.Slime || slimepart != SlimePart.Face) return;
        if (m_slimeController != null)
        {
            if (targetimg != null) targetimg.enabled = false;
            if (targetsr != null) targetsr.enabled = false;
            return;
        }

        faceSprite = faceSprite != null ? faceSprite : DoodleLabSkin.Sprite("Face");
        ApplyLayer(faceSprite);
    }

    public void ApplyCostume(Sprite costumeSprite)
    {
        if (category != Category.Slime || slimepart != SlimePart.Costume) return;
        if (m_slimeController != null) return;
        ApplyLayer(costumeSprite);
    }

    private void ApplyLayer(Sprite sprite)
    {
        Transform body = transform.parent != null ? transform.parent.Find("Color") : null;
        if (targetimg != null)
        {
            targetimg.sprite = sprite;
            targetimg.enabled = sprite != null;
            targetimg.color = Color.white;
            targetimg.raycastTarget = false;
            targetimg.preserveAspect = true;
            if (body != null && body.TryGetComponent<Image>(out var bodyImage))
            {
                RectTransform source = bodyImage.rectTransform;
                RectTransform layer = targetimg.rectTransform;
                layer.anchorMin = source.anchorMin;
                layer.anchorMax = source.anchorMax;
                layer.pivot = source.pivot;
                layer.sizeDelta = source.sizeDelta;
                layer.anchoredPosition3D = source.anchoredPosition3D;
                layer.localRotation = source.localRotation;
                layer.localScale = source.localScale;
            }
        }
        if (targetsr != null)
        {
            targetsr.sprite = sprite;
            targetsr.enabled = sprite != null;
            targetsr.color = Color.white;
            if (body != null)
            {
                transform.localPosition = body.localPosition;
                transform.localRotation = body.localRotation;
                transform.localScale = body.localScale;
            }
        }
    }

    public void ApplyMap(ThemeData themeData)
    {
        if (category != Category.Map || themeData == null) return;

        Sprite spriteToApply = mappart switch
        {
            MapPart.Menu => themeData.MenuSprite,
            MapPart.GameTable => themeData.TableSprite,
            MapPart.Slime => themeData.SlimeSprite,
            _ => null
        };

        if (spriteToApply == null) return;

        if (targetimg != null)
        {
            targetimg.sprite = spriteToApply;
            targetimg.color = Color.white;
            targetimg.type = mappart == MapPart.GameTable ? Image.Type.Sliced : Image.Type.Simple;
            targetimg.preserveAspect = mappart == MapPart.Slime;
            targetimg.pixelsPerUnitMultiplier = mappart == MapPart.GameTable ? 3.5f : 2f;
        }
        if (targetsr != null) { targetsr.sprite = spriteToApply; targetsr.color = Color.white; }
    }
}
