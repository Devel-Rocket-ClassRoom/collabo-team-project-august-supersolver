using PPS.Core;
using UnityEngine;

public sealed class GameComposite : MonoSingleton<GameComposite>
{
    [SerializeField] ThemeAssetView themeView;
    protected override void Awake()
    {
        // 리소스 로더
        var loader = new AddressableLoader();
        loader.Init();

        ServiceLocator.Register<IResourceLoader>(loader);

        // 테마 리소스 레포지토리
        var repo = new ThemeRepository();
        repo.Init(loader);

        ServiceLocator.Register<IThemeRepository>(repo);

        // 테마별 View 오케스트레이터?
        themeView.Init(repo);

    }
}
