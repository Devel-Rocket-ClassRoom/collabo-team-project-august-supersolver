using PPS.Core;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

public class StageButton : MonoBehaviour
{
    static bool locked = false;
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
    
    public async void OnClicked()
    {
        if (StageContext.Instance == null) return;
        if (stageIdx == -1) return;
        if (locked) return;

        locked = true;
        Debug.Log("로딩시작");
        await StageContext.Instance.LoadStageAsync(stageIdx);
        Debug.Log("로딩끝" + StageContext.Instance.Stages[stageIdx].StageId); // 스테이지 20개 없으면 에러남. 
        locked = false;
    }
}
