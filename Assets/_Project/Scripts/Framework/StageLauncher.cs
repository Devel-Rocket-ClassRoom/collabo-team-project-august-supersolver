using Cysharp.Threading.Tasks;
using PPS.Core;
using PPS.DrawingTool;

/// <summary>
/// 스테이지에 들어가는 순서 한 벌. 진입 지점마다 순서를
/// 따로 적으면 한쪽만 바뀌어 어긋난다.
/// </summary>
public static class StageLauncher
{
    public static async UniTask Enter(StageEntry entry)
    {
        if (!ServiceLocator.TryGet<IThemeRepository>(out var repo)) return;

        var stageData = repo.Asset.Stages[entry.Stage];

        // 툴바 잠금이 이것을 읽는다. 패널을 먼저 띄우면
        // 직전 스테이지 번호로 잠금을 계산한다.
        StageSelection.SelectStage(entry.Stage);

        await UIManager.Instance.ShowScene<DrawingToolSceneUI>();

        StageLoader.SetStage(stageData);
        TutorialViewer.SetStage(entry);
    }
}
