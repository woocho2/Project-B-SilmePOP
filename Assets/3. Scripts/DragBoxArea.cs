using System.Collections.Generic;
using UnityEngine;

public class DragBoxArea : MonoBehaviour
{
    private List<SlimeController> overlappingSlimes = new List<SlimeController>();

    private readonly Color colorRed = new Color(1f, 0.2f, 0.2f, 0.8f);
    private readonly Color colorGreen = new Color(0.2f, 1f, 0.2f, 0.8f);

    private void OnEnable()
    {
        ClearSlimesHighlight();
        overlappingSlimes.Clear();
    }

    private void OnDisable()
    {
        ClearSlimesHighlight();
        overlappingSlimes.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SlimeController slime = other.GetComponent<SlimeController>();
        if (slime != null && !overlappingSlimes.Contains(slime))
        {
            overlappingSlimes.Add(slime);
            slime.SetHighlight(true);
            UpdateHighlightsColor();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        SlimeController slime = other.GetComponent<SlimeController>();
        if (slime != null && overlappingSlimes.Contains(slime))
        {
            slime.SetHighlight(false);
            overlappingSlimes.Remove(slime);
            UpdateHighlightsColor();
        }
    }

    private void UpdateHighlightsColor()
    {
        int currentSum = GetCurrentSum();
        Color targetColor = (currentSum == 10) ? colorGreen : colorRed;

        foreach (SlimeController slime in overlappingSlimes)
        {
            if (slime != null)
            {
                slime.SetHighlightColor(targetColor);
            }
        }
    }

    private int GetCurrentSum()
    {
        int sum = 0;
        foreach (SlimeController slime in overlappingSlimes)
        {
            if (slime != null)
            {
                sum += slime.CurrentNumber;
            }
        }
        return sum;
    }

    public void EvaluateSlimes(ref int totalScore)
    {
        int sum = GetCurrentSum();

        if (sum == 10)
        {
            SlimeController[] slimesToDestroy = overlappingSlimes.ToArray();
            int destroyedCount = 0;

            foreach (SlimeController slime in slimesToDestroy)
            {
                if (slime != null)
                {
                    // Destroy는 프레임 종료 시점에 객체를 파괴하므로, 논리적 즉시 계산을 위해 비활성화 우선 적용
                    slime.gameObject.SetActive(false);
                    Destroy(slime.gameObject);
                    destroyedCount++;
                }
            }

            totalScore += destroyedCount;
            Debug.Log($"합이 10입니다! 파괴된 슬라임: {destroyedCount}개, 현재 총점: {totalScore}");

            // 슬라임이 파괴된 직후 남은 10 조합이 있는지 즉시 확인
            if (SlimeManager.Instance != null && !SlimeManager.Instance.HasAvailableMatches())
            {
                Debug.Log("더 이상 맞출 수 있는 슬라임이 없습니다");

                bool canUseSkill = false;
                if (UIManager_Game.Instance != null)
                {
                    // UIManager_Game에 추가된 메서드로 스킬 잔여 횟수 체크
                    canUseSkill = UIManager_Game.Instance.HasAnySkillLeft();
                }

                // 스킬을 하나도 쓸 수 없는 상황이라면 게임 오버 처리
                if (!canUseSkill && GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
            }
        }
        else if (sum > 0)
        {
            Debug.Log($"선택된 슬라임의 합은 {sum}입니다. 10이 아닙니다.");
        }

        ClearSlimesHighlight();
        overlappingSlimes.Clear();
    }

    private void ClearSlimesHighlight()
    {
        foreach (SlimeController slime in overlappingSlimes)
        {
            if (slime != null)
            {
                slime.SetHighlight(false);
            }
        }
    }
}