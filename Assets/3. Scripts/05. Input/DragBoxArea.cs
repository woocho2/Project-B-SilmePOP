using System.Collections.Generic;
using UnityEngine;

// 끄는 동안 겹친 칸을 강조하기만 함 (초록 = 목표 합, 빨강 = 아님). 판정은 DragManager가 함.
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

    private int GetCurrentSum()
    {
        List<int> cells = new List<int>();
        foreach (SlimeController slime in overlappingSlimes)
        {
            cells.Add(slime.Cell);
        }
        return MatchFinder.SumNumbers(GameManager.Instance.Board.GetSlimesInCells(cells));
    }

    private void UpdateHighlightsColor()
    {
        int currentSum = GetCurrentSum();
        Color targetColor = (currentSum == GameManager.Instance.TargetSum) ? colorGreen : colorRed;

        foreach (SlimeController slime in overlappingSlimes)
        {
            slime.SetHighlightColor(targetColor);
        }
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
