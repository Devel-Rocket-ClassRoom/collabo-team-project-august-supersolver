using PPS.Core;
using System;
using UnityEngine;

public class StageSelectRow : MonoBehaviour
{
    public const int StagePerRow = CurrentStageIndex.StagePerRow;
    [SerializeField] StageButton[] buttons;

    private void OnValidate()
    {
        if (buttons == null) buttons = new StageButton[StagePerRow];
        if(buttons.Length != 3)
            Array.Resize<StageButton>(ref buttons, StagePerRow);
    }
    public void OnUpdate(int startIdx, int maxStageIdx)
    {
        for(int i = 0; i < StagePerRow; i++)
        {
            buttons[i].OnUpdate(startIdx + i, maxStageIdx);
        }
    }
}
