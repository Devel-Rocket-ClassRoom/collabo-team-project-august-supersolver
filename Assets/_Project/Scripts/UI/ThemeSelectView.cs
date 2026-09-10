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
        for (int i = 0; i < catalog.Asset.Count; i++)
        {
            int idx = i;
            ThemeAssetEntry entry = catalog.Asset[i];

            // 해금 판정은 진척도 저장 규약이 정리될 때까지 미룬다.
            // LastClearedStageIndex 로는 테마를 구분할 수 없다.
            GetButton(i).Init(entry.Spr_SelectButton, true,
                () => SelectTheme(idx, entry.label).Forget());
        }
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
