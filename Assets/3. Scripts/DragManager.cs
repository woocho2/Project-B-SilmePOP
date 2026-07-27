using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DragManager : MonoBehaviour
{
    [Header("World Drag Box Prefab/Object")]
    [SerializeField] private GameObject dragBoxPrefab; // SpriteRenderer + BoxCollider2D + DragBoxArea가 붙은 프리팹
    private GameObject currentDragBox;
    private DragBoxArea dragBoxArea;

    public event UnityAction<int> OnScoreChanged;
    public int totalScore = 0;

    private Vector2 startPosWorld;
    private bool isDragging = false;

    private void Start()
    {
        // 드래그 박스 프리팹을 미리 1개 생성해 두고 비활성화 처리 (성능 최적화)
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

                // 충돌체에 감지된 슬라임 계산 실행
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

    // 월드 좌표 기준 드래그 박스의 위치와 크기를 수정하는 메서드
    private void UpdateDragBoxTransform(Vector2 start, Vector2 end)
    {
        // 1. 위치 설정 (두점의 중간값)
        Vector2 center = (start + end) / 2f;
        currentDragBox.transform.position = center;

        // 2. 스케일 크기 설정 (기본 Sprite 스케일이 1x1 단위일 때)
        float width = Mathf.Abs(start.x - end.x);
        float height = Mathf.Abs(start.y - end.y);

        currentDragBox.transform.localScale = new Vector3(width, height, 1f);
    }
}