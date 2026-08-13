using PPS.Core;
using UnityEngine;
using UnityEngine.UI;

public class ThemeAssetView : MonoSingleton<ThemeAssetView>
{
    [SerializeField] Image StageSelectBackground;
    IThemeRepository _repo;
    public void Init(IThemeRepository repo)
    {
        _repo = repo;
        _repo.OnLoaded -= OnThemeChanged;
        _repo.OnLoaded += OnThemeChanged;
    }
    void OnThemeChanged()
    {
        StageSelectBackground.sprite = _repo.Asset.StageSelectBackground;
    }
}
