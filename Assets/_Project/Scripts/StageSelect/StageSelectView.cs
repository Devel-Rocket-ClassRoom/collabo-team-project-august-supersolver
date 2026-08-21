using PPS.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectView : UIScene
{
    const int MaxStageNum = CurrentStageIndex.StagePerTheme;
    [SerializeField] Transform parent;
    [SerializeField] Image background;
    [SerializeField] StageButton prefab;

    private List<StageButton> buttons;
    IThemeRepository _repo;
    public override void Initialize()
    {
        base.Initialize();
        buttons = new();
        for (int i = 0; i < MaxStageNum; i++)
        {
            buttons.Add(Instantiate(prefab, parent));
        }
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
        int lastClearedStageIndex = ServiceLocator.Get<IUserDataRepository>().Data.LastClearedStageIndex;
        for (int i = 0; i < MaxStageNum; i++)
        {
            buttons[i].OnUpdate(
                stageIdx:    i,
                maxStageIdx: maxStageIndex,
                lastCleared: lastClearedStageIndex
            );
        }
    }
    void UpdateTheme()
    {
        background.sprite = _repo.Asset.StageSelectBackground;
    }
}
