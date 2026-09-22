using PPS.Core;
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

        // 설정 팝업은 애드레서블로 따로 로드돼
        // 인스펙터로 이 씬 오브젝트를 못 묶는다.
        UIManager.Instance.GetPanel<HomeConfirmView>().BindConfirm(OnClickedHome);
    }
    public async void OnClickedHome()
    {
        if (locked) return;
        locked = true;
        await UIManager.Instance.ShowScene<StageSelectView>();

        // 설정 위에 확인 팝업이 겹쳐 있어 하나만 벗기면
        // 스테이지 선택 화면 위에 설정창이 남는다.
        await UIManager.Instance.HideAllPopups(true);
        locked = false;
    }
    public async void OnClickedNext()
    {
        if (locked) return;
        locked = true;

        var manifest = ServiceLocator.Get<AssetManifest>();
        StageEntry current = StageSelection.Current;

        if (current.Stage >= manifest.GetStageNum(current.Theme) - 1)
        {
            // 테마의 끝이라 갈 다음 칸이 없다. 보상창을 먼저
            // 걷어야 테마 선택창의 해금 연출이 가려지지 않는다.
            await UIManager.Instance.HideAllPopups(true);
            await UIManager.Instance.ShowScene<ThemeSelectView>();
        }
        else
        {
            await StageLauncher.Enter(new StageEntry(current.Theme, current.Stage + 1));
            await UIManager.Instance.HidePopup(false);
        }

        locked = false;
    }
}
