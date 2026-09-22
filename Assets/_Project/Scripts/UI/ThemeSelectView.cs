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
        int unlocked = UnlockedThemeCount();
        for (int i = 0; i < catalog.Asset.Count; i++)
        {
            int idx = i;
            ThemeAssetEntry entry = catalog.Asset[i];

            GetButton(i).Init(entry.Spr_SelectButton, i < unlocked,
                () => EnterTheme(idx).Forget());
        }
    }

    /// 다음 테마는 앞 테마를 끝까지 깨야 열린다. 지금 열려
    /// 있는 다음 스테이지가 속한 테마가 곧 마지막 해금 테마다.
    int UnlockedThemeCount()
    {
        var manifest = ServiceLocator.Get<AssetManifest>();
        return ServiceLocator.Get<IUserDataRepository>().Data
            .LastCleared.Next(manifest).Theme + 1;
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
