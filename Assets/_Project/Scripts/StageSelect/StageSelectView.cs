using Cysharp.Threading.Tasks;
using PPS.Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectView : UIScene
{
    [SerializeField] Transform parent;
    [SerializeField] Image background;
    [SerializeField] StageButton prefab;

    /// 지금 어느 테마에 있는지 알리는 그림.
    [SerializeField] Image currentThemeIcon;

    /// 이 테마에서 등급별로 모은 별 개수. 스테이지 버튼에
    /// 그려지는 별과 같은 것을 센다.
    [SerializeField] TextMeshProUGUI bronzeStarLabel;
    [SerializeField] TextMeshProUGUI silverStarLabel;
    [SerializeField] TextMeshProUGUI goldStarLabel;

    /// 숫자 옆에 붙는 별 그림. 별은 테마마다 달라서
    /// 인스펙터에 박지 않고 로드된 테마에서 가져온다.
    [SerializeField] Image bronzeStarIcon;
    [SerializeField] Image silverStarIcon;
    [SerializeField] Image goldStarIcon;

    /// 이 테마를 다 깼을 때만 뜨는 다음 테마 버튼.
    [SerializeField] Button nextThemeButton;

    /// 어느 테마로 가는지 알리는 그림. 테마 선택 버튼과
    /// 같은 그림을 써서 두 화면이 같은 것을 가리키게 한다.
    [SerializeField] Image nextThemeIcon;

    /// 첫 테마가 아닐 때만 뜨는 앞 테마 버튼.
    [SerializeField] Button prevThemeButton;

    /// 어느 테마로 돌아가는지 알리는 그림.
    [SerializeField] Image prevThemeIcon;

    /// 테마 선택 버튼 그림이 들어 있는 카탈로그.
    [SerializeField] ThemeAssetCatalog catalog;

    private List<StageButton> buttons;
    IThemeRepository _repo;
    IUserDataRepository _userRepo;
    AssetManifest _manifest;

    /// 지금 테마의 버튼 개수. 테마마다 달라서 UpdateTheme
    /// 이 맞춰 두고 ApplyStages 가 그대로 쓴다.
    int stageNum;

    /// 테마를 갈아 끼우는 중인가. 로드가 끝나기 전에
    /// 또 누르면 테마를 두 칸 건너뛴다.
    bool moving;

    public override void Initialize()
    {
        base.Initialize();
        buttons = new();
        _manifest = ServiceLocator.Get<AssetManifest>();
        _repo = ServiceLocator.Get<IThemeRepository>();
        _userRepo = ServiceLocator.Get<IUserDataRepository>();
        _repo.OnLoaded -= UpdateTheme;
        _repo.OnLoaded += UpdateTheme;

        nextThemeButton.onClick.RemoveAllListeners();
        nextThemeButton.onClick.AddListener(() => MoveTheme(1).Forget());

        prevThemeButton.onClick.RemoveAllListeners();
        prevThemeButton.onClick.AddListener(() => MoveTheme(-1).Forget());

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

        if (ServiceLocator.TryGet<ISoundManager>(out var sound))
            sound.PlayBgm(BgmType.Stage);

        ApplyStages();
    }

    /// <summary>
    /// 지금 테마의 칸 상태를 다시 칠한다. 테마가 갈릴 때도
    /// 불린다 — 그때는 씬이 이미 떠 있어 OnBeforeShow 가 없다.
    /// </summary>
    void ApplyStages()
    {
        ThemeModel theme = _repo.Asset;
        UserData data = _userRepo.Data;
        int maxStageIdx = theme.Stages.Count;
        int themeIdx = StageSelection.Current.Theme;
        int firstLocked = FirstLockedIndex(data, themeIdx, maxStageIdx);

        int bronze = 0, silver = 0, gold = 0;

        for (int i = 0; i < stageNum; i++)
        {
            var entry = new StageEntry(themeIdx, i);
            bool isLocked = i >= firstLocked;

            // 잠긴 칸은 기록을 들추지 않는다. 열리지 않은 자리에
            // 별이 붙어 있으면 그것 자체가 버그 신호다.
            StageClearData clear = isLocked ? null : _userRepo.ClearOf(entry);
            bool cleared = clear != null && clear.IsCleared;

            if (cleared)
            {
                switch (clear.StarGrade)
                {
                    case InkGrade.Gold: gold += clear.BestStars; break;
                    case InkGrade.Silver: silver += clear.BestStars; break;
                    default: bronze += clear.BestStars; break;
                }
            }

            StageButtonViewModel vm = new StageButtonViewModel(
                entry: entry,
                isLocked: isLocked,
                stars: cleared ? clear.BestStars : 0,
                lockedSprite: theme.SprLocked,
                starSprite: StarOf(theme, cleared ? clear.StarGrade : InkGrade.Bronze)
            );

            buttons[i].ApplyView(vm);
        }

        currentThemeIcon.sprite = ThemeSprite(themeIdx);

        bronzeStarLabel.text = bronze.ToString();
        silverStarLabel.text = silver.ToString();
        goldStarLabel.text = gold.ToString();

        bronzeStarIcon.sprite = theme.SprStarBronze;
        silverStarIcon.sprite = theme.SprStarSilver;
        goldStarIcon.sprite = theme.SprStarGold;

        bool hasNext = HasNextTheme(data, themeIdx);
        bool hasPrev = themeIdx > 0;

        nextThemeButton.gameObject.SetActive(hasNext);
        nextThemeIcon.gameObject.SetActive(hasNext);

        if (hasNext) nextThemeIcon.sprite = ThemeSprite(themeIdx + 1);

        prevThemeButton.gameObject.SetActive(hasPrev);
        prevThemeIcon.gameObject.SetActive(hasPrev);

        if (hasPrev) prevThemeIcon.sprite = ThemeSprite(themeIdx - 1);
    }

    /// 이 테마를 끝까지 깼고 뒤에 갈 테마가 남았는가.
    /// 마지막 테마에서는 버튼을 띄우지 않는다.
    bool HasNextTheme(UserData data, int themeIdx)
    {
        if (themeIdx + 1 >= _manifest.ThemeCount) return false;

        return data.HasPlayed
            && data.LastCleared >= new StageEntry(themeIdx, stageNum - 1);
    }

    /// 그 테마의 선택 버튼 그림. 카탈로그가 매니페스트보다
    /// 짧으면 비워 둔다 — 여기서 터뜨릴 일은 아니다.
    Sprite ThemeSprite(int themeIdx)
        => themeIdx < catalog.Asset.Count ? catalog.Asset[themeIdx].Spr_SelectButton : null;

    /// <summary>
    /// 앞뒤 테마로 넘어간다. 다음 테마 쪽에 아직 못 본 해금
    /// 연출이 남았으면 테마 선택창으로 보낸다 — 연출은
    /// 거기서만 나온다.
    /// </summary>
    async UniTaskVoid MoveTheme(int step)
    {
        if (moving) return;

        moving = true;
        SetThemeMoveInteractable(false);
        try
        {
            if (step > 0 && ThemeProgress.HasPendingUnlock(_userRepo.Data, _manifest))
            {
                await UIManager.Instance.ShowScene<ThemeSelectView>();
                return;
            }

            int themeIdx = StageSelection.Current.Theme + step;

            StageSelection.SelectTheme(themeIdx);
            await _repo.LoadAsync(_manifest.GetThemeLabel(themeIdx));

            // 씬이 이미 떠 있어 ShowScene 이 아무것도 하지
            // 않는다. 바뀐 테마를 직접 다시 칠한다.
            ApplyStages();
        }
        finally
        {
            moving = false;
            SetThemeMoveInteractable(true);
        }
    }

    void SetThemeMoveInteractable(bool value)
    {
        nextThemeButton.interactable = value;
        prevThemeButton.interactable = value;
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
