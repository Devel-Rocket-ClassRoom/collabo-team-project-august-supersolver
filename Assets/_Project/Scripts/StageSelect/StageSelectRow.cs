using System;
using UnityEngine;

public class StageSelectRow : MonoBehaviour
{
    public const int StageNumInARow = 3;
    [SerializeField] StageButton[] buttons;

    private void OnValidate()
    {
        if (buttons == null) buttons = new StageButton[StageNumInARow];
        if(buttons.Length != 3)
            Array.Resize<StageButton>(ref buttons, StageNumInARow);
    }
    public void OnUpdate(int startIdx, int maxIdx)
    {
        for(int i = 0; i < StageNumInARow; i++)
        {
            buttons[i].OnUpdate(startIdx + i, maxIdx);
        }
    }
}
