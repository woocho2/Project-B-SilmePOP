using UnityEngine;
using UnityEngine.UI;

// TreeEditor 네임스페이스 삭제 완료 (빌드 에러 방지)

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
        Face
    }

    public enum MapPart
    {
        Menu,
        GameTable,
        SliderBar,
        TimerBar
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
        // 이벤트 관리를 단일 메서드로 통합하여 중복 제거
        ManageEvents(true);

        // 씬 활성화 시점에 저장된 데이터를 즉시 불러옴 (Start()에서의 중복 호출 제거)
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
                UIManager_Menu.Instance.OnMapChanged += ApplyMap;
            }
            else
            {
                UIManager_Menu.Instance.OnColorChanged -= ApplyColor;
                UIManager_Menu.Instance.OnFaceChanged -= ApplyFace;
                UIManager_Menu.Instance.OnMapChanged -= ApplyMap;
            }
        }

        if (UIManager_Game.Instance != null)
        {
            if (subscribe)
            {
                UIManager_Game.Instance.OnColorChanged += ApplyColor;
                UIManager_Game.Instance.OnFaceChanged += ApplyFace;
                UIManager_Game.Instance.OnMapChanged += ApplyMap;
            }
            else
            {
                UIManager_Game.Instance.OnColorChanged -= ApplyColor;
                UIManager_Game.Instance.OnFaceChanged -= ApplyFace;
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
        if (category != Category.Slime || slimepart != SlimePart.Color) return;

        if (targetimg != null) targetimg.color = color;
        if (targetsr != null) targetsr.color = color;

        if (slimeController != null)
        {
            slimeController.SetSlimeColor(color);
        }
    }

    public void ApplyFace(Sprite faceSprite)
    {
        if (category != Category.Slime || slimepart != SlimePart.Face) return;
        if (faceSprite == null) return;

        if (targetimg != null) targetimg.sprite = faceSprite;
        if (targetsr != null) targetsr.sprite = faceSprite;
    }


    public void ApplyMap(ThemeData themeData)
    {
        if (category != Category.Map || themeData == null) return;

        Sprite spriteToApply = mappart switch
        {
            MapPart.Menu => themeData.mainMenuSprite,
            MapPart.GameTable => themeData.optionAndTableSprite,
            MapPart.SliderBar => themeData.mapFillSprite,
            MapPart.TimerBar => themeData.timerFillSprite,
            _ => null
        };

        if (spriteToApply == null) return;

        if (targetimg != null) targetimg.sprite = spriteToApply;
        if (targetsr != null) targetsr.sprite = spriteToApply;
    }
}