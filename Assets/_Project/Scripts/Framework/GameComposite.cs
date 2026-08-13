using Cysharp.Threading.Tasks;
using PPS.Core;
using UnityEngine;

public sealed class GameComposite : MonoSingleton<GameComposite>
{
    protected override async void Awake()
    {
        float elapsed = Time.time;
        base.Awake();
        Debug.Log("로딩 시작");
        await UIManager.Instance.ShowInitialLoading();
        await UniTask.WaitForSeconds(3f);

        Debug.Log($"로딩 화면 띄우기 완료 elapsed: {Time.time - elapsed}");

        // 리소스 로더
        var loader = new AddressableLoader();
        loader.Init();

        ServiceLocator.Register<IResourceLoader>(loader);
        Debug.Log($"리소스 로더 등록 완료 elapsed: {Time.time - elapsed}");

        // 테마 리소스 레포지토리
        var repo = new ThemeRepository();
        repo.Init(loader);

        ServiceLocator.Register<IThemeRepository>(repo);
        Debug.Log($"테마 레포지토리 등록 완료 elapsed: {Time.time - elapsed}");

        await repo.LoadAsync(ThemeLabel.KOREA);
        Debug.Log($"테마 로딩 완료 elapsed: {Time.time - elapsed}");

        await UIManager.Instance.InitializeAsync();

        await UIManager.Instance.HideInitialLoading();
        await UIManager.Instance.ShowScene<StageSelectView>();
    }
}
