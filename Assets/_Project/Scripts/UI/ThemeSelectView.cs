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
    [SerializeField] RectTransform body;
    [SerializeField] Transform buttonParent;
    [SerializeField] ThemeSelectButton themeButtonPrefab;

    [Header("Animation")]
    [SerializeField] float showAnimOffset = 10f;
    [SerializeField] float showAnimDuration = .5f;

    List<ThemeSelectButton> buttons = new();

    /// 테마 에셋을 로드하는 중인가. 끝나기 전에 다른 테마를
    /// 누르면 CurrentTheme 과 실제 로드된 에셋이 어긋난다.
    bool loading;

    protected override async UniTask OnShowAnimation()
    {
        await base.OnShowAnimation();
        UpdateThemeButton();
        float dest = body.anchoredPosition.y;
        body.anchoredPosition += Vector2.up * showAnimOffset;
        var task = body.DOAnchorPosY(dest, showAnimDuration)
            .SetEase(Ease.OutCubic);

        await task.AsyncWaitForCompletion();
    }
    void UpdateThemeButton()
    {
        int unlocked = UnlockedThemeCount();
        for (int i = 0; i < catalog.Asset.Count; i++)
        {
            int idx = i;
            ThemeAssetEntry entry = catalog.Asset[i];

            GetButton(i).Init(entry.Spr_SelectButton, i < unlocked,
                () => SelectTheme(idx, entry.label).Forget());
        }
    }

    /// 다음 테마는 앞 테마를 끝까지 깨야 열린다. 지금 열려
    /// 있는 다음 스테이지가 속한 테마가 곧 마지막 해금 테마다.
    int UnlockedThemeCount()
    {
        int lastCleared = ServiceLocator.Get<IUserDataRepository>().Data.LastClearedStageIndex;
        return CurrentStageIndex.ThemeOf(lastCleared + 1) + 1;
    }
    async UniTask SelectTheme(int themeIdx, ThemeLabel label)
    {
        if (loading) return;

        loading = true;
        SetButtonsInteractable(false);
        try
        {
            CurrentStageIndex.CurrentTheme = themeIdx;
            await ServiceLocator.Get<IThemeRepository>().LoadAsync(label);
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
