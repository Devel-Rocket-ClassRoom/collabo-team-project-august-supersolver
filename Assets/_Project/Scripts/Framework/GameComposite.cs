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
        IUserDataStorage userDataStorage = new PlayerPrefsUserDataStorage();
        IUserDataService userDataService = new UserDataService(userDataStorage);
        ServiceLocator.Register(userDataService);   // 세이브 로드용
        Debug.Log($"유저 데이터 서비스 등록 완료 elapsed: {Time.time - elapsed}");
        UserDataLoadResult result = await userDataService.LoadAsync();
        if(result.Success == false)
        {
            Debug.LogWarning("유저 데이터 로드 실패: " + result.ErrorMessage);
        }else
        {
            Debug.Log($"유저 데이터 로드 완료 elapsed: {Time.time - elapsed}");
        }
        IUserDataRepository userrepo = new UserDataRepository(result.Data);
        ServiceLocator.Register(userrepo); // 조회용
        Debug.Log($"유저 데이터 서비스 등록 완료 elapsed: {Time.time - elapsed}");

        // 리소스 로더
        IResourceLoader loader = new AddressableLoader();
        loader.Init();
        ServiceLocator.Register(loader);
        Debug.Log($"리소스 로더 등록 완료 elapsed: {Time.time - elapsed}");

        // 테마 리소스 레포지토리
        IThemeRepository themerepo = new ThemeRepository();
        themerepo.Init(loader);
        ServiceLocator.Register(themerepo);
        Debug.Log($"테마 레포지토리 등록 완료 elapsed: {Time.time - elapsed}");

        await themerepo.LoadAsync(ThemeLabel.KOREA);
        //(ThemeLabel)(Mathf.Clamp(userrepo.Data.LastClearedStageIndex / 20, 0, 1))
        Debug.Log($"테마 로딩 완료 elapsed: {Time.time - elapsed}");


        await UIManager.Instance.InitializeAsync();

        ServiceLocator.Register<IRewardView>(UIManager.Instance.GetPanel<RewardView>());

        await UIManager.Instance.HideInitialLoading();
        await UIManager.Instance.ShowScene<StageSelectView>();
    }
}
