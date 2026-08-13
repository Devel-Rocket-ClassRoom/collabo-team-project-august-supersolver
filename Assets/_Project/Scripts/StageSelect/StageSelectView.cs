using PPS.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectView : UIScene
{
    const int StagePerTheme = 20;
    const int StagePerRow = StageSelectRow.StagePerRow;
    const int MaxRow = StagePerTheme / StageSelectRow.StagePerRow + 1;

    [SerializeField] Transform parent;
    [SerializeField] ScrollRect scroll;
    [SerializeField] Image background;

    [Header("Prefab")]
    [SerializeField] StageSelectRow RowLR;
    [SerializeField] StageSelectRow RowRL;

    List<StageSelectRow> rowPool;
    IThemeRepository _repo;
    public override void Initialize()
    {
        base.Initialize();
        rowPool = new();

        for (int i = 0; i < MaxRow; i++)
        {
            bool isL2R = i % 2 == 0;

            var item = isL2R
                ? Instantiate(RowLR.gameObject, parent)
                : Instantiate(RowRL.gameObject, parent);

            rowPool.Add(item.GetComponent<StageSelectRow>());
        }
        _repo = ServiceLocator.Get<IThemeRepository>();
        BindRepo(_repo);
    }
    public override void OnBeforeShow()
    {
        base.OnBeforeShow();
        for (int i = 0; i < MaxRow; i++)
        {
            rowPool[i].OnUpdate(i * StagePerRow, _repo.Asset.Stages.Count);
        }
    }
    void BindRepo(IThemeRepository repo)
    {
        _repo = repo;
        _repo.OnLoaded -= OnThemeChanged;
        _repo.OnLoaded += OnThemeChanged;
    }
    void OnThemeChanged()
    {
        background.sprite = _repo.Asset.StageSelectBackground;
    }
}
