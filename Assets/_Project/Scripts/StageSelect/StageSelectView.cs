using PPS.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectView : UIScene
{
    [SerializeField] Transform parent;
    [SerializeField] Image background;
    [SerializeField] StageButton prefab;

    private List<StageButton> buttons;
    IThemeRepository _repo;
    AssetManifest _manifest;

    /// 지금 테마의 버튼 개수. 테마마다 달라서 UpdateTheme
    /// 이 맞춰 두고 OnBeforeShow 가 그대로 쓴다.
    int stageNum;

    public override void Initialize()
    {
        base.Initialize();
        buttons = new();
        _manifest = ServiceLocator.Get<AssetManifest>();
        _repo = ServiceLocator.Get<IThemeRepository>();
        _repo.OnLoaded -= UpdateTheme;
        _repo.OnLoaded += UpdateTheme;
        UpdateTheme();
    }
    private void OnDestroy()
    {
        if(_repo != null)
            _repo.OnLoaded -= UpdateTheme;
    }
    public override void OnBeforeShow()
    {
        base.OnBeforeShow();
        int maxStageIndex = _repo.Asset.Stages.Count;
        StageEntry lastCleared = ServiceLocator.Get<IUserDataRepository>().Data.LastCleared;
        for (int i = 0; i < stageNum; i++)
        {
            buttons[i].OnUpdate(
                stageIdx:    i,
                maxStageIdx: maxStageIndex,
                lastCleared: lastCleared
            );
        }
    }
    void UpdateTheme()
    {
        background.sprite = _repo.Asset.StageSelectBackground;

        stageNum = _manifest.GetStageNum(StageSelection.Current.Theme);
        while (buttons.Count < stageNum)
            buttons.Add(Instantiate(prefab, parent));
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].gameObject.SetActive(i < stageNum);
    }
}
