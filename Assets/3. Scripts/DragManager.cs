using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DragManager : MonoBehaviour
{
    public static DragManager Instance { get; private set; } // 외부 접근용 싱글톤 추가

    [Header("World Drag Box Prefab/Object")]
    [SerializeField] private GameObject dragBoxPrefab;
    private GameObject currentDragBox;
    private DragBoxArea dragBoxArea;

    public event UnityAction<int> OnScoreChanged;
    public int totalScore = 0;

    private Vector2 startPosWorld;
    private bool isDragging = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (dragBoxPrefab != null)
        {
            currentDragBox = Instantiate(dragBoxPrefab);
            dragBoxArea = currentDragBox.GetComponent<DragBoxArea>();
            currentDragBox.SetActive(false);
        }
        else
        {
            Debug.LogError("DragManager에 dragBoxPrefab이 할당되지 않았습니다!");
        }
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // 슬라임 단일 파괴 스킬이 켜져 있는 동안에는 드래그 로직을 완전히 차단합니다.
        if (SlimeManager.Instance != null && SlimeManager.Instance.IsDestroySkillActive)
        {
            return;
        }

        // 1. 드래그 시작
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isDragging = true;

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            startPosWorld = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            if (currentDragBox != null)
            {
                currentDragBox.SetActive(true);
                UpdateDragBoxTransform(startPosWorld, startPosWorld);
            }
        }

        // 2. 드래그 진행 중
        if (Mouse.current.leftButton.isPressed && isDragging)
        {
            if (currentDragBox != null)
            {
                Vector2 currentPosWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                UpdateDragBoxTransform(startPosWorld, currentPosWorld);
            }
        }

        // 3. 드래그 종료 및 조건 판정
        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;

            if (dragBoxArea != null)
            {
                int previousScore = totalScore;
                dragBoxArea.EvaluateSlimes(ref totalScore);

                if (previousScore != totalScore)
                {
                    OnScoreChanged?.Invoke(totalScore);
                }
            }

            if (currentDragBox != null)
            {
                currentDragBox.SetActive(false);
            }
        }
    }

    public void ResetDragManager()
    {
        totalScore = 0;
        OnScoreChanged?.Invoke(totalScore);

        if (currentDragBox != null)
        {
            currentDragBox.SetActive(false);
        }
    }

    private void UpdateDragBoxTransform(Vector2 start, Vector2 end)
    {
        Vector2 center = (start + end) / 2f;
        currentDragBox.transform.position = center;

        float width = Mathf.Abs(start.x - end.x);
        float height = Mathf.Abs(start.y - end.y);

        currentDragBox.transform.localScale = new Vector3(width, height, 1f);
    }

    /// <summary>
    /// 외부 스크립트에서 점수를 강제로 올릴 때 호출하는 메서드입니다.
    /// </summary>
    public void AddScore(int scoreToAdd)
    {
        totalScore += scoreToAdd;
        OnScoreChanged?.Invoke(totalScore);
    }
}