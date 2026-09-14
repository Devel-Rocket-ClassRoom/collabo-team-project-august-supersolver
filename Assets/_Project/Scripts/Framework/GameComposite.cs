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
        
        Debug.Log($"로딩 화면 띄우기 완료 elapsed: {Time.time - elapsed}");

        // 유저 데이터 레포지토리
        // 조회: IUserDataRepository
        // 세이브로드: IUserDataService
        IUserDataStorage userDataStorage = new FakeUserDataStorage();
        IUserDataService userDataService = new UserDataService(userDataStorage);
        ServiceLocator.Register(userDataService);
        Debug.Log($"유저 데이터 서비스 등록 완료 elapsed: {Time.time - elapsed}");
        UserDataLoadResult result = await userDataService.LoadAsync();
        if(result.Success == false)
        {
            Debug.LogError("유저 데이터 로드 실패: " + result.ErrorMessage);
        }
        else
        {
            Debug.Log($"유저 데이터 로드 완료 elapsed: {Time.time - elapsed}");
            IUserDataRepository userrepo = new UserDataRepository(result.Data);
            ServiceLocator.Register(userrepo);
            Debug.Log($"유저 데이터 서비스 등록 완료 elapsed: {Time.time - elapsed}");
        }

        // 리소스 로더
        IResourceLoader loader = new AddressableLoader();
        loader.Init();
        ServiceLocator.Register(loader);
        Debug.Log($"리소스 로더 등록 완료 elapsed: {Time.time - elapsed}");

        // 테마 카탈로그. 테마 번들을 받기 전에 개수·라벨을
        // 알아야 해서 테마와 다른 번들에 둔다.
        var manifest = await loader.LoadAssetAsync<AssetManifest>("AssetManifest");
        ServiceLocator.Register(manifest);
        Debug.Log($"에셋 매니페스트 등록 완료 elapsed: {Time.time - elapsed}");

        // 테마 리소스 레포지토리
        IThemeRepository themerepo = new ThemeRepository();
        themerepo.Init(loader);
        ServiceLocator.Register(themerepo);
        Debug.Log($"테마 레포지토리 등록 완료 elapsed: {Time.time - elapsed}");

        // 진척도가 가리키는 테마로 바로 들어간다. StageSelectView
        // 가 초기화될 때 이미 로드된 테마를 읽어야 한다.
        int themeIdx = EntryThemeIndex(manifest);
        StageSelection.SelectTheme(themeIdx);
        await themerepo.LoadAsync(manifest.GetThemeLabel(themeIdx));
        Debug.Log($"테마 로딩 완료 elapsed: {Time.time - elapsed}");


        await UIManager.Instance.InitializeAsync();

        ServiceLocator.Register<IRewardView>(UIManager.Instance.GetPanel<RewardView>());

        await UIManager.Instance.HideInitialLoading();

        // 한 번도 안 깬 계정은 선택 화면을 건너뛰고 1-1 로
        // 넣는다. 진척도를 못 읽었으면 선택 화면으로 둔다.
        if (!ServiceLocator.TryGet<IUserDataRepository>(out var repo) || repo.Data.HasPlayed)
            await UIManager.Instance.ShowScene<StageSelectView>();
        else
            await StageLauncher.Enter(new StageEntry(0, 0));
    }

    /// 다음에 풀 스테이지가 속한 테마. 유저 데이터를 못 읽었으면
    /// 첫 테마로 둔다 — 진척도를 모르는 채로 뒤 테마를 열 수 없다.
    static int EntryThemeIndex(AssetManifest manifest)
    {
        if (!ServiceLocator.TryGet<IUserDataRepository>(out var repo)) return 0;

        // Next 가 마지막 테마에서 자기 자신을 준다.
        // 따로 자를 필요가 없다.
        return repo.Data.LastCleared.Next(manifest).Theme;
    }
}
