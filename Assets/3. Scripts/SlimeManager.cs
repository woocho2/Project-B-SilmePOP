using UnityEngine;
using System.Collections.Generic; // List 자료구조 사용을 위해 추가

public class SlimeManager : MonoBehaviour
{
    public static SlimeManager Instance { get; private set; } // 외부 접근을 위한 싱글톤

    [Header("Slime Settings")]
    [SerializeField] GameObject slimePrefab;

    [Header("Grid Settings")]
    [SerializeField] int columns = 17;       // 가로 칸 수 (X축)
    [SerializeField] int rows = 10;          // 세로 칸 수 (Y축)

    [SerializeField] Vector2 areaSize = new Vector2(10f, 6f);

    private SlimeController[,] slimeGrid; // 슬라임을 추적할 2차원 배열

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        GenerateSlimeGrid();
    }

    private void GenerateSlimeGrid()
    {
        if (slimePrefab == null)
        {
            Debug.LogError("슬라임 프리팹이 할당되지 않았습니다.");
            return;
        }

        // 새 게임이 시작될 때마다 배열 초기화
        slimeGrid = new SlimeController[columns, rows];

        Vector2 startPos = new Vector2(
            transform.position.x - (areaSize.x / 2f),
            transform.position.y - (areaSize.y / 2f)
        );

        float spacingX = columns > 1 ? areaSize.x / (columns - 1) : 0f;
        float spacingY = rows > 1 ? areaSize.y / (rows - 1) : 0f;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector2 spawnPosition = new Vector2(
                    startPos.x + (x * spacingX),
                    startPos.y + (y * spacingY)
                );

                GameObject newSlime = Instantiate(slimePrefab, spawnPosition, Quaternion.identity, transform);
                slimeGrid[x, y] = newSlime.GetComponent<SlimeController>(); // 배열에 슬라임 저장
            }
        }
    }

    public void ResetSlimeManager()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        GenerateSlimeGrid();
    }

    /// <summary>
    /// 현재 격자판에서 합계 10을 만들 수 있는 사각형 조합이 단 하나라도 남아있는지 검사합니다.
    /// (DragBoxArea에서 게임 오버 판정을 위해 호출)
    /// </summary>
    public bool HasAvailableMatches()
    {
        if (slimeGrid == null) return false;

        for (int startX = 0; startX < columns; startX++)
        {
            for (int startY = 0; startY < rows; startY++)
            {
                for (int endX = startX; endX < columns; endX++)
                {
                    for (int endY = startY; endY < rows; endY++)
                    {
                        int sum = 0;
                        bool hasActiveSlime = false;

                        for (int x = startX; x <= endX; x++)
                        {
                            for (int y = startY; y <= endY; y++)
                            {
                                SlimeController slime = slimeGrid[x, y];
                                if (slime != null && slime.gameObject.activeInHierarchy)
                                {
                                    sum += slime.CurrentNumber;
                                    hasActiveSlime = true;
                                }
                            }
                        }

                        if (sum > 10) break;

                        if (hasActiveSlime && sum == 10)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 게임 중 스킬 버튼을 눌렀을 때 힌트를 표시합니다.
    /// (UIManager_Game에서 호출)
    /// </summary>
    public void ShowHint()
    {
        if (slimeGrid == null) return;

        // 이전에 표시된 힌트를 모두 끕니다.
        ClearAllHints();

        for (int startX = 0; startX < columns; startX++)
        {
            for (int startY = 0; startY < rows; startY++)
            {
                for (int endX = startX; endX < columns; endX++)
                {
                    for (int endY = startY; endY < rows; endY++)
                    {
                        int sum = 0;
                        bool hasActiveSlime = false;

                        for (int x = startX; x <= endX; x++)
                        {
                            for (int y = startY; y <= endY; y++)
                            {
                                SlimeController slime = slimeGrid[x, y];
                                if (slime != null && slime.gameObject.activeInHierarchy)
                                {
                                    sum += slime.CurrentNumber;
                                    hasActiveSlime = true;
                                }
                            }
                        }

                        if (sum > 10) break;

                        if (hasActiveSlime && sum == 10)
                        {
                            // 해당 사각형 영역 안의 슬라임들의 힌트를 켭니다.
                            for (int x = startX; x <= endX; x++)
                            {
                                for (int y = startY; y <= endY; y++)
                                {
                                    SlimeController slime = slimeGrid[x, y];
                                    if (slime != null && slime.gameObject.activeInHierarchy)
                                    {
                                        slime.SetHintHighlight(true);
                                    }
                                }
                            }

                            return;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 모든 슬라임의 힌트 표시를 초기화합니다.
    /// </summary>
    public void ClearAllHints()
    {
        if (slimeGrid == null) return;

        foreach (SlimeController slime in slimeGrid)
        {
            if (slime != null)
            {
                slime.SetHintHighlight(false);
            }
        }
    }

    /// <summary>
    /// 현재 화면에 남아있는 슬라임들의 숫자를 수집하여 무작위로 재배치합니다.
    /// </summary>
    public void MixSlimes()
    {
        if (slimeGrid == null) return;

        List<int> activeNumbers = new List<int>();

        // 1. 현재 화면에 활성화(존재)되어 있는 슬라임들의 번호를 모두 리스트에 수집
        foreach (SlimeController slime in slimeGrid)
        {
            if (slime != null && slime.gameObject.activeInHierarchy)
            {
                activeNumbers.Add(slime.CurrentNumber);
            }
        }

        // 남은 슬라임이 없다면 실행 취소
        if (activeNumbers.Count == 0) return;

        // 2. 피셔-예이츠 셔플(Fisher-Yates Shuffle) 알고리즘을 이용해 리스트의 요소들을 무작위로 섞음
        for (int i = 0; i < activeNumbers.Count; i++)
        {
            int temp = activeNumbers[i];
            int randomIndex = Random.Range(i, activeNumbers.Count);
            activeNumbers[i] = activeNumbers[randomIndex];
            activeNumbers[randomIndex] = temp;
        }

        // 3. 무작위로 섞인 숫자 배열을 활성화된 슬라임들에게 순서대로 재할당
        int index = 0;
        foreach (SlimeController slime in slimeGrid)
        {
            if (slime != null && slime.gameObject.activeInHierarchy)
            {
                slime.SetNumber(activeNumbers[index]);

                // 번호가 재배치되었으므로 기존에 켜져있던 힌트 하이라이트 해제
                slime.SetHintHighlight(false);
                slime.SetHighlight(false);

                index++;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}