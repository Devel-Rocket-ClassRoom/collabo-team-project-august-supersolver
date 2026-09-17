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
    IUserDataRepository _userRepo;
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
        _userRepo = ServiceLocator.Get<IUserDataRepository>();
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

        ThemeModel theme = _repo.Asset;
        UserData data = _userRepo.Data;
        int maxStageIdx = theme.Stages.Count;
        int themeIdx = StageSelection.Current.Theme;
        int firstLocked = FirstLockedIndex(data, themeIdx, maxStageIdx);

        for (int i = 0; i < stageNum; i++)
        {
            var entry = new StageEntry(themeIdx, i);
            bool isLocked = i >= firstLocked;

            // 잠긴 칸은 기록을 들추지 않는다. 열리지 않은 자리에
            // 별이 붙어 있으면 그것 자체가 버그 신호다.
            StageClearData clear = isLocked ? null : _userRepo.ClearOf(entry);
            bool cleared = clear != null && clear.IsCleared;

            StageButtonViewModel vm = new StageButtonViewModel(
                entry: entry,
                isLocked: isLocked,
                stars: cleared ? clear.BestStars : 0,
                lockedSprite: theme.SprLocked,
                starSprite: StarOf(theme, cleared ? clear.StarGrade : InkGrade.Bronze)
            );

            buttons[i].ApplyView(vm);
        }
    }

    /// 잠기기 시작하는 칸. 깬 자리와 그 다음 한 칸만 열리고
    /// 열린 칸은 항상 앞에서부터 이어지므로 경계 하나로 족하다.
    int FirstLockedIndex(UserData data, int themeIdx, int maxStageIdx)
    {
        int firstLocked;

        if (!data.HasPlayed)
        {
            // 아무것도 안 깬 계정은 LastCleared 가 (0,0) 이라
            // 1-1 을 깬 것과 구분되지 않는다. 첫 칸만 연다.
            firstLocked = themeIdx == 0 ? 1 : 0;
        }
        else if (themeIdx < data.LastCleared.Theme)
        {
            // 지나온 테마는 전부 열려 있다.
            firstLocked = maxStageIdx;
        }
        else if (themeIdx == data.LastCleared.Theme)
        {
            // 깬 자리와 그 다음 한 칸까지 열린다.
            firstLocked = data.LastCleared.Stage + 2;
        }
        else
        {
            // 아직 닿지 않은 테마는 다음 자리가 이 테마의
            // 첫 칸일 때만 열린다.
            firstLocked = data.LastCleared.Next(_manifest) == new StageEntry(themeIdx, 0)
                        ? 1 : 0;
        }

        return Mathf.Min(firstLocked, maxStageIdx);
    }

    static Sprite StarOf(ThemeModel theme, int grade)
    {
        switch (grade)
        {
            case InkGrade.Gold: return theme.SprStarGold;
            case InkGrade.Silver: return theme.SprStarSilver;
            default: return theme.SprStarBronze;
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
