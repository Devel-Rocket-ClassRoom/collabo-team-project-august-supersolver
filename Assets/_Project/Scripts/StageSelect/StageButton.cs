using PPS.Core;
using TMPro;
using UnityEngine;

public class StageButton : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI stageNumText;
    int stageIdx = -1;

    public void OnUpdate(int stageIdx, int maxIdx)
    {
        if(stageIdx < 0 || stageIdx >= maxIdx)
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

        StageContext.Instance.StageIndex = stageIdx;
    }
}
