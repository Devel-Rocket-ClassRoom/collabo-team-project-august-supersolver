using Cysharp.Threading.Tasks;
using PPS.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ThemeLabel
{
    KOREA, JAPAN
}
public class ThemeRepository : MonoSingleton<ThemeRepository>
{
    private IResourceLoader _loader;
    private IResourceHandle _handle;

    private ThemeLabel currentTheme;
    public ThemeModel ThemeData { get; private set; }

    bool locked = false;

    private readonly Dictionary<ThemeLabel, string> _map = new()
    {
        { ThemeLabel.KOREA, "Korea" },
        { ThemeLabel.JAPAN, "Japan" },
    };
    public async UniTask LoadAsync(ThemeLabel theme)
    {
        locked = true;
        Debug.Log("[테마 에셋 로드] 로딩시작");
        if (locked)
        {
            Debug.LogWarning("[테마 에셋 로드 실패] 재진입 가드");
            return;
        }
        if (_handle != null && currentTheme == theme)
        {
            Debug.LogWarning("[테마 에셋 로드 실패] 이미 로드된 에셋을 또 로드하려고 시도함");
            locked = false;
            return;
        }
        if (_loader == null && !ServiceLocator.TryGet<IResourceLoader>(out _loader))
        {
            Debug.LogWarning("[테마 에셋 로드 실패] 로더 없음. 서비스 로케이터에 리소스 로더가 없음");
            locked = false;
            return;
        }
        if(!_map.TryGetValue(theme, out var label))
        {
            Debug.LogWarning("[테마 에셋 로드 실패] 테마 라벨 미등록. 이 파일의 _map에 등록해야함");
            locked = false;
            return;
        }

        if (_handle != null)
            await _loader.Unload(_handle);

        _handle = await _loader.LoadAsync(label);

        // 텍스트 에셋은 전부 스테이지 데이터임! 구분하려면 prefix 추가 필요해짐!
        var Stages = _handle.Assets
                .OfType<TextAsset>()
                .Select(asset => StageData.FromJson(asset.text))
                .ToList();

        ThemeData = new ThemeModel(Stages);
        currentTheme = theme;
        locked = false;
        Debug.Log("[테마 에셋 로드] 로딩종료");
    }

}
