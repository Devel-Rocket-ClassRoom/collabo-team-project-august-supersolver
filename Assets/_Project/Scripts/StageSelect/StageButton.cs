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
        if (stageIdx == -1) return;
        if (locked) return;

        if (ServiceLocator.TryGet<IThemeRepository>(out var repo))
        {
            StageSelectManager.Instance.LastSelectedStage 
                = repo.Asset.Stages[stageIdx];
        }
       
    }
}
