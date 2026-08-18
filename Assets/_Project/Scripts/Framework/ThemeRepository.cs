using Cysharp.Threading.Tasks;
using PPS.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        EnsureLoad(theme, out var label);

        if (_handle != null)
            await _loader.Unload(_handle);

        _handle = await _loader.LoadAsync(label);

        // 텍스트 에셋은 전부 스테이지 데이터임! 구분하려면 prefix 추가 필요해짐!
        _asset = _handle.Assets.OfType<ThemeAssetSet>().Single();


        // Adapter
        var Stages = _asset.stages
            .Select(stageText => StageData.FromJson(stageText.text))
            .ToList();
        var stageSelectBackground = _asset.stageSelectBackground;
        var playBackground = _asset.playBackground;
        var mapEditStyle = _asset.MapStyle;

        Asset = new ThemeModel(
            Stages, stageSelectBackground, playBackground, mapEditStyle, _asset.tutorials);


        currentTheme = theme;
        _locked = false;
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
