using PPS.Core;
using PPS.DrawingTool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageButton : MonoBehaviour
{
    static bool locked = false;
    [Header("Sprite")]
    [SerializeField] Image img;
    [SerializeField] Sprite Spr_Locked;
    [SerializeField] Sprite Spr_Unlocked;
    [SerializeField] Sprite Spr_Star1;
    [SerializeField] Sprite Spr_Star2;
    [SerializeField] Sprite Spr_Star3;

    [Header("txt")]
    [SerializeField] TextMeshProUGUI stageNumText;
    int stageIdx = -1;

    public void OnUpdate(int stageIdx, int maxStageIdx)
    {
        if(stageIdx < 0 || stageIdx >= maxStageIdx)
        {
            img.sprite = Spr_Locked;
            stageNumText.text = "";
            this.stageIdx = -1;
            return;
        }
        img.sprite = Spr_Unlocked;
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
            TutorialViewer.SetStage(stageIdx);
        }
        locked = false;
    }
}
