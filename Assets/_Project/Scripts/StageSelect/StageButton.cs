using PPS.Core;
using PPS.DrawingTool;
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
            stageNumText.text = "X";
            this.stageIdx = -1;
            return;
        }
        this.stageIdx = stageIdx;
        stageNumText.text = (stageIdx + 1).ToString();
    }
    
    public async void OnClicked()
    {
        if (stageIdx == -1) return;
        if (locked) return;
        locked = true;
        if (ServiceLocator.TryGet<IThemeRepository>(out var repo))
        {
            var StageData = repo.Asset.Stages[stageIdx];

            await UIManager.Instance.ShowScene<DrawingToolSceneUI>();

            StageLoader.SetStage(StageData);
        }
        locked = false;
    }
}
