using PPS.Core;
using PPS.DrawingTool;
using UnityEngine;

public class StageSceneLoaderOnClick : MonoBehaviour
{
    bool locked;
    private void Start()
    {
        ServiceLocator.Get<IRewardView>().BindButtonListener(
            retry: null,    // DI 때문에 StageFlow.cs 에서 주입
            home: OnClickedHome,
            next: OnClickedNext);
    }
    public async void OnClickedHome()
    {
        if (locked) return;
        locked = true;
        await UIManager.Instance.ShowScene<StageSelectView>();
        await UIManager.Instance.HidePopup(true);
        locked = false;
    }
    public async void OnClickedNext()
    {
        if (locked) return;
        if (CurrentStageIndex.CurrentStage >= CurrentStageIndex.StagePerTheme - 1) return;
        locked = true;

        var stageIdx = ++CurrentStageIndex.CurrentStage;
        if (ServiceLocator.TryGet<IThemeRepository>(out var repo))
        {
            var StageData = repo.Asset.Stages[stageIdx];

            await UIManager.Instance.ShowScene<DrawingToolSceneUI>();

            StageLoader.SetStage(StageData);
            TutorialViewer.SetStage(stageIdx);

            await UIManager.Instance.HidePopup(false);
        }
        locked = false;
    }
}
