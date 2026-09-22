using Cysharp.Threading.Tasks;
using DG.Tweening;
using PPS.Core;
using System.Collections.Generic;
using UnityEngine;

public class ThemeSelectView : UIScene
{
    [Header("Data")]
    [SerializeField] ThemeAssetCatalog catalog;

    [Header("UGUI")]
    [SerializeField] Transform buttonParent;
    [SerializeField] ThemeSelectButton themeButtonPrefab;

    [Header("Animation")]
    [SerializeField] float showAnimDuration = .3f;

    /// 버튼이 하나씩 터지는 간격.
    [SerializeField] float popInterval = .06f;

    List<ThemeSelectButton> buttons = new();

    /// 테마 에셋을 로드하는 중인가. 끝나기 전에 다른 테마를
    /// 누르면 CurrentTheme 과 실제 로드된 에셋이 어긋난다.
    bool loading;

    protected override async UniTask OnShowAnimation()
    {
        if (ServiceLocator.TryGet<ISoundManager>(out var sound))
            sound.PlayBgm(BgmType.Stage);

        await base.OnShowAnimation();
        UpdateThemeButton();

        await PopInButtons();
        await PlayPendingUnlock();
    }

    /// <summary>
    /// 새로 열린 테마의 자물쇠를 푼다. 한 번 보여 준 뒤
    /// 유저 데이터에 적어 다시 나오지 않게 한다.
    /// </summary>
    async UniTask PlayPendingUnlock()
    {
        var manifest = ServiceLocator.Get<AssetManifest>();
        var repo = ServiceLocator.Get<IUserDataRepository>();

        if (!ThemeProgress.HasPendingUnlock(repo.Data, manifest)) return;

        int unlocked = ThemeProgress.UnlockedCount(repo.Data, manifest);
        int themeIdx = unlocked - 1;

        // 카탈로그가 매니페스트보다 짧으면 그릴 버튼이 없다.
        if (themeIdx < 0 || themeIdx >= buttons.Count) return;

        await buttons[themeIdx].PlayUnlock();

        repo.Data.ThemeUnlockAnimShown = unlocked;
        ServiceLocator.Get<IUserDataService>().SaveAsync(repo.Data).Forget();
    }

    /// <summary>
    /// 버튼을 하나씩 부풀려 띄운다. 자리는 레이아웃 그룹이
    /// 잡으므로 위치를 건드리면 되돌려진다 — 스케일만 만진다.
    /// </summary>
    async UniTask PopInButtons()
    {
        Sequence sequence = DOTween.Sequence().SetLink(gameObject);

        for (int i = 0; i < buttons.Count; i++)
        {
            Transform button = buttons[i].transform;
            button.localScale = Vector3.zero;

            sequence.Insert(i * popInterval,
                button.DOScale(1f, showAnimDuration).SetEase(Ease.OutBack));
        }

        await sequence.AsyncWaitForCompletion();
    }
    void UpdateThemeButton()
    {
        var manifest = ServiceLocator.Get<AssetManifest>();
        UserData data = ServiceLocator.Get<IUserDataRepository>().Data;

        // 해금 연출이 남아 있으면 그 테마는 잠긴 채로 띄운다.
        // 자물쇠가 풀리는 걸 봐야 새로 열렸다는 게 읽힌다.
        int unlocked = ThemeProgress.UnlockedCount(data, manifest);
        int shown = ThemeProgress.HasPendingUnlock(data, manifest) ? unlocked - 1 : unlocked;

        for (int i = 0; i < catalog.Asset.Count; i++)
        {
            int idx = i;
            ThemeAssetEntry entry = catalog.Asset[i];

            GetButton(i).Init(entry.Spr_SelectButton, i < shown,
                () => EnterTheme(idx).Forget());
        }
    }
    async UniTask EnterTheme(int themeIdx)
    {
        if (loading) return;

        loading = true;
        SetButtonsInteractable(false);
        try
        {
            StageSelection.SelectTheme(themeIdx);
            await ServiceLocator.Get<IThemeRepository>().LoadAsync(
                ServiceLocator.Get<AssetManifest>().GetThemeLabel(themeIdx));
            await UIManager.Instance.ShowScene<StageSelectView>();
        }
        finally
        {
            loading = false;
            SetButtonsInteractable(true);
        }
    }
    void SetButtonsInteractable(bool value)
    {
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].SetInteractable(value);
    }
    ThemeSelectButton GetButton(int idx)
    {
        ThemeSelectButton obj = null;
        if (buttons.Count > idx)
            obj = buttons[idx];
        else
        {
            obj = Instantiate(themeButtonPrefab.gameObject, buttonParent).GetComponent<ThemeSelectButton>();
            buttons.Add(obj);
        }
        return obj;
    }
}
