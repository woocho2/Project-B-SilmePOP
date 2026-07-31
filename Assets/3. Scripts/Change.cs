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
        Costume // 코스튬 부위 추가
    }

    public enum MapPart
    {
        Menu,
        GameTable,
        Slime,
        SliderBar,
    }

    [SerializeField] private Category category = Category.Slime;
    [SerializeField] private SlimePart slimepart;
    [SerializeField] private MapPart mappart;

    private Image targetimg;
    private SpriteRenderer targetsr;
    private SlimeController slimeController;

    private void Awake()
    {
        targetimg = GetComponent<Image>();
        targetsr = GetComponent<SpriteRenderer>();
        slimeController = GetComponentInParent<SlimeController>();
    }

    private void OnEnable()
    {
        ManageEvents(true);
        ApplySavedData();
    }

    private void OnDisable()
    {
        ManageEvents(false);
    }

    private void ManageEvents(bool subscribe)
    {
        if (UIManager_Menu.Instance != null)
        {
            if (subscribe)
            {
                UIManager_Menu.Instance.OnColorChanged += ApplyColor;
                UIManager_Menu.Instance.OnFaceChanged += ApplyFace;
                UIManager_Menu.Instance.OnCostumeChanged += ApplyCostume; // 코스튬 이벤트 구독
                UIManager_Menu.Instance.OnMapChanged += ApplyMap;
            }
            else
            {
                UIManager_Menu.Instance.OnColorChanged -= ApplyColor;
                UIManager_Menu.Instance.OnFaceChanged -= ApplyFace;
                UIManager_Menu.Instance.OnCostumeChanged -= ApplyCostume; // 코스튬 이벤트 해제
                UIManager_Menu.Instance.OnMapChanged -= ApplyMap;
            }
        }

        if (UIManager_Game.Instance != null)
        {
            if (subscribe)
            {
                UIManager_Game.Instance.OnColorChanged += ApplyColor;
                UIManager_Game.Instance.OnFaceChanged += ApplyFace;
                UIManager_Game.Instance.OnCostumeChanged += ApplyCostume; // 코스튬 이벤트 구독
                UIManager_Game.Instance.OnMapChanged += ApplyMap;
            }
            else
            {
                UIManager_Game.Instance.OnColorChanged -= ApplyColor;
                UIManager_Game.Instance.OnFaceChanged -= ApplyFace;
                UIManager_Game.Instance.OnCostumeChanged -= ApplyCostume; // 코스튬 이벤트 해제
                UIManager_Game.Instance.OnMapChanged -= ApplyMap;
            }
        }
    }

    public void ApplySavedData()
    {
        if (category == Category.Slime)
        {
            if (slimepart == SlimePart.Color)
            {
                ApplyColor(ChangeData.SelectedColor);
            }
            else if (slimepart == SlimePart.Face)
            {
                ApplyFace(ChangeData.SelectedFace);
            }
            else if (slimepart == SlimePart.Costume) // 코스튬 초기 데이터 불러오기
            {
                ApplyCostume(ChangeData.SelectedCostume);
            }
        }
        else if (category == Category.Map)
        {
            if (mappart == MapPart.SliderBar)
            {
                ApplyColor(ChangeData.SelectedColor);
            }
            else if (ChangeData.SelectedMapTheme != null)
            {
                ApplyMap(ChangeData.SelectedMapTheme);
            }
        }
    }

    public void ApplyColor(Color color)
    {
        if (category == Category.Slime && slimepart == SlimePart.Color)
        {
            if (targetimg != null) targetimg.color = color;
            if (targetsr != null) targetsr.color = color;

            if (slimeController != null)
            {
                slimeController.SetSlimeColor(color);
            }
        }
        else if (category == Category.Map && (mappart == MapPart.SliderBar))
        {
            if (targetimg != null) targetimg.color = color;
            if (targetsr != null) targetsr.color = color;
        }
    }

    public void ApplyFace(Sprite faceSprite)
    {
        if (category != Category.Slime || slimepart != SlimePart.Face) return;
        if (faceSprite == null) return;

        if (targetimg != null) targetimg.sprite = faceSprite;
        if (targetsr != null) targetsr.sprite = faceSprite;
    }

    // 코스튬 적용 메서드 추가
    public void ApplyCostume(Sprite costumeSprite)
    {
        if (category != Category.Slime || slimepart != SlimePart.Costume) return;
        if (costumeSprite == null) return;

        if (targetimg != null) targetimg.sprite = costumeSprite;
        if (targetsr != null) targetsr.sprite = costumeSprite;
    }

    public void ApplyMap(ThemeData themeData)
    {
        if (category != Category.Map || themeData == null) return;
        if (mappart == MapPart.SliderBar) return;

        Sprite spriteToApply = mappart switch
        {
            MapPart.Menu => themeData.MenuSprite,
            MapPart.GameTable => themeData.TableSprite,
            MapPart.Slime => themeData.SlimeSprite,
            _ => null
        };

        if (spriteToApply == null) return;

        if (targetimg != null) targetimg.sprite = spriteToApply;
        if (targetsr != null) targetsr.sprite = spriteToApply;
    }
}