using Cysharp.Threading.Tasks;
using PPS.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
public class ThemeRepository : IThemeRepository
{
    public event Action OnLoaded;
    public ThemeModel Asset { get; private set; }

    private readonly Dictionary<ThemeLabel, string> _map = new()
    {
        { ThemeLabel.KOREA, "Korea" },
        { ThemeLabel.JAPAN, "Japan" },
    };
    private IResourceLoader _loader;
    private IResourceHandle _handle;

    private ThemeLabel currentTheme;

    private ThemeAssetSet _asset;
    private ThemeStageSet _stageSet;

    // 테마와 무관해서 한 번 받아 두고 계속 쓴다.
    private TutorialSet _tutorials;
    private FixedTutorialSet _fixedTutorials;

    private bool _locked = false;

    public void Init(IResourceLoader loader)
    {
        _loader = loader;
        _loader.AfterLoad += OnResourceLoaded;
    }
    private void OnResourceLoaded() => OnLoaded?.Invoke();
    public async UniTask LoadAsync(ThemeLabel theme)
    {
        Debug.Log("[테마 에셋 로드] 로딩시작");

        // 재진입 가드, 같은 에셋 로드, 테마 라벨 등록 여부 검사
        if (!EnsureLoad(theme, out var label)) return;

        // 로드가 터져도 잠금은 풀어야 한다. 남으면 이후
        // 모든 테마 로드가 재진입 가드에 막힌다.
        try
        {
            if (_handle != null)
            {
                await _loader.Unload(_handle);
                Addressables.Release(_stageSet);
            }

            _handle = await _loader.LoadAsync(label);
            _asset = _handle.Assets.OfType<ThemeAssetSet>().Single();

            _stageSet = await _loader.LoadAssetAsync<ThemeStageSet>("StageSet_" + label);
            _tutorials ??= await _loader.LoadAssetAsync<TutorialSet>("TutorialSet");
            _fixedTutorials ??= await _loader.LoadAssetAsync<FixedTutorialSet>("FixedTutorialSet");


            // Adapter
            var Stages = _stageSet.Stages
                .Select(stageText => StageData.FromJson(stageText.text))
                .ToList();
            var stageSelectBackground = _asset.stageSelectBackground;
            var playBackground = _asset.playBackground;
            var mapEditStyle = _asset.MapStyle;

            Asset = new ThemeModel(
                Stages, stageSelectBackground, playBackground, mapEditStyle,
                _tutorials.Tutorials, _fixedTutorials.FixedTutorials,
                _asset.SprLocked, _asset.SprStarBronze, _asset.SprStarSilver, _asset.SprStarGold);


            currentTheme = theme;
        }
        finally
        {
            _locked = false;
        }
        Debug.Log("[테마 에셋 로드] 로딩종료");
    }


    // 재진입 가드, 같은 에셋 로드, 테마 라벨 등록 여부 검사
    private bool EnsureLoad(ThemeLabel theme, out string label)
    {
        label = "";
        if (_locked)
        {
            Debug.LogWarning("[테마 에셋 로드 실패] 재진입 가드");
            return false;
        }
        _locked = true;
        if (_handle != null && currentTheme == theme)
        {
            Debug.LogWarning("[테마 에셋 로드 실패] 이미 로드된 에셋을 또 로드하려고 시도함");
            _locked = false;
            return false;
        }
        if (!_map.TryGetValue(theme, out label))
        {
            Debug.LogWarning("[테마 에셋 로드 실패] 테마 라벨 미등록. 이 파일의 _map에 등록해야함");
            _locked = false;
            return false;
        }
        return true;
    }
}
