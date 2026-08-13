using PPS.Core;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

public class StageButton : MonoBehaviour
{
    static bool locked = false;
    [SerializeField] TextMeshProUGUI stageNumText;
    int stageIdx = -1;

    public void OnUpdate(int stageIdx, int maxStageIdx)
    {
        if(stageIdx < 0 || stageIdx >= maxStageIdx)
        {
            stageNumText.text = "no";
            this.stageIdx = -1;
            return;
        }
        this.stageIdx = stageIdx;
        stageNumText.text = (stageIdx + 1).ToString();
    }
    
    public void OnClicked()
    {
        if (StageContext.Instance == null) return;
        if (stageIdx == -1) return;
        if (locked) return;

        var stage = ThemeRepository.Instance.ThemeData.Stages[stageIdx];
        StageContext.Instance.LastSelectedStage = stage;
    }
}
