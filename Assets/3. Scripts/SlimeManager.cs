using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class SlimeManager : MonoBehaviour
{
    public static SlimeManager Instance { get; private set; }

    [Header("Slime Settings")]
    [SerializeField] GameObject slimePrefab;

    [Header("Grid Settings")]
    [SerializeField] int columns = 17;
    [SerializeField] int rows = 10;

    [SerializeField] Vector2 areaSize = new Vector2(10f, 6f);

    private SlimeController[,] slimeGrid;
    private int[,] prefixSums;

    public bool IsDestroySkillActive { get; private set; } = false;

    private SlimeController currentHoveredSlime;

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

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (IsDestroySkillActive && Mouse.current != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            SlimeController targetSlime = null;
            if (hit.collider != null)
            {
                targetSlime = hit.collider.GetComponent<SlimeController>();
            }

            if (currentHoveredSlime != targetSlime)
            {
                if (currentHoveredSlime != null)
                {
                    currentHoveredSlime.SetDestroyHoverHighlight(false);
                }

                if (targetSlime != null && targetSlime.gameObject.activeInHierarchy)
                {
                    targetSlime.SetDestroyHoverHighlight(true);
                }

                currentHoveredSlime = targetSlime;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (targetSlime != null && targetSlime.gameObject.activeInHierarchy)
                {
                    DestroySpecificSlimeAndAddScore(targetSlime);
                }

                IsDestroySkillActive = false;
                ChangeAllSlimesColliderSize(0.1f, 0.1f);

                if (currentHoveredSlime != null)
                {
                    currentHoveredSlime.SetDestroyHoverHighlight(false);
                    currentHoveredSlime = null;
                }
            }
        }
    }

    public void ActivateDestroySkill()
    {
        IsDestroySkillActive = true;
        ChangeAllSlimesColliderSize(1f, 1f);
        Debug.Log("파괴 스킬 장전 완료: 타겟 슬라임을 클릭하세요.");
    }

    private void ChangeAllSlimesColliderSize(float sizeX, float sizeY)
    {
        if (slimeGrid == null) return;

        foreach (SlimeController slime in slimeGrid)
        {
            if (slime != null && slime.gameObject.activeInHierarchy)
            {
                slime.SetColliderSize(sizeX, sizeY);
            }
        }
    }

    private void DestroySpecificSlimeAndAddScore(SlimeController targetSlime)
    {
        SoundManager.Play(SoundEffect.Destroy);
        targetSlime.gameObject.SetActive(false);
        Destroy(targetSlime.gameObject);

        if (DragManager.Instance != null)
        {
            DragManager.Instance.AddScore(1);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckAndTriggerGameOver();
        }
    }

    private void GenerateSlimeGrid()
    {
        if (slimePrefab == null)
        {
            Debug.LogError("슬라임 프리팹이 할당되지 않았습니다.");
            return;
        }

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
                slimeGrid[x, y] = newSlime.GetComponent<SlimeController>();
            }
        }
    }

    public void ResetSlimeManager()
    {
        IsDestroySkillActive = false;
        currentHoveredSlime = null;

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        GenerateSlimeGrid();
    }

    private bool TryFindMatch(out int matchStartX, out int matchStartY, out int matchEndX, out int matchEndY)
    {
        matchStartX = matchStartY = matchEndX = matchEndY = -1;

        if (slimeGrid == null) return false;

        if (prefixSums == null || prefixSums.GetLength(0) != columns + 1 || prefixSums.GetLength(1) != rows + 1)
            prefixSums = new int[columns + 1, rows + 1];
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                SlimeController slime = slimeGrid[x, y];
                int value = slime != null && slime.gameObject.activeInHierarchy ? slime.CurrentNumber : 0;
                prefixSums[x + 1, y + 1] = value + prefixSums[x, y + 1]
                    + prefixSums[x + 1, y] - prefixSums[x, y];
            }
        }

        for (int startX = 0; startX < columns; startX++)
        {
            for (int startY = 0; startY < rows; startY++)
            {
                for (int endX = startX; endX < columns; endX++)
                {
                    for (int endY = startY; endY < rows; endY++)
                    {
                        int sum = prefixSums[endX + 1, endY + 1] - prefixSums[startX, endY + 1]
                            - prefixSums[endX + 1, startY] + prefixSums[startX, startY];

                        if (sum > 10) break;

                        if (sum == 10)
                        {
                            matchStartX = startX;
                            matchStartY = startY;
                            matchEndX = endX;
                            matchEndY = endY;
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    public bool HasAvailableMatches()
    {
        return TryFindMatch(out _, out _, out _, out _);
    }

    public void ShowHint()
    {
        if (slimeGrid == null) return;

        ClearAllHints();

        if (TryFindMatch(out int startX, out int startY, out int endX, out int endY))
        {
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
        }
    }

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

    public void MixSlimes()
    {
        if (slimeGrid == null) return;

        List<int> activeNumbers = new List<int>();

        foreach (SlimeController slime in slimeGrid)
        {
            if (slime != null && slime.gameObject.activeInHierarchy)
            {
                activeNumbers.Add(slime.CurrentNumber);
            }
        }

        if (activeNumbers.Count == 0) return;

        for (int i = 0; i < activeNumbers.Count; i++)
        {
            int temp = activeNumbers[i];
            int randomIndex = Random.Range(i, activeNumbers.Count);
            activeNumbers[i] = activeNumbers[randomIndex];
            activeNumbers[randomIndex] = temp;
        }

        int index = 0;
        foreach (SlimeController slime in slimeGrid)
        {
            if (slime != null && slime.gameObject.activeInHierarchy)
            {
                slime.SetNumber(activeNumbers[index]);

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
