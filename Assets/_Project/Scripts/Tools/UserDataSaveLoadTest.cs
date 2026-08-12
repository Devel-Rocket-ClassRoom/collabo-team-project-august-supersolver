using PPS.Core;
using UnityEngine;

public class UserDataSaveLoadTest : MonoBehaviour
{
    // Play Mode가 시작되면 저장과 불러오기를 차례대로 검증한다.
    async void Start()
    {
        // PlayerPrefs 저장 구현체를 생성한다.
        IUserDataStorage storage = new PlayerPrefsUserDataStorage();

        // 저장 흐름과 이벤트를 관리하는 서비스를 생성한다.
        UserDataService service = new UserDataService(storage);

        // 이벤트가 호출되는 순서를 Console에서 확인한다.
        service.BeforeSave += () => Debug.Log("1. BeforeSave");
        service.AfterSave += result =>
            Debug.Log($"2. AfterSave: Success={result.Success}");

        service.BeforeLoad += () => Debug.Log("3. BeforeLoad");
        service.AfterLoad += result =>
            Debug.Log($"4. AfterLoad: Success={result.Success}");

        // 저장과 복원을 확인할 테스트 데이터를 만든다.
        UserData originalData = new UserData
        {
            LastClearedStageIndex = 2
        };

        // 두 번째 스테이지의 클리어 기록을 추가한다.
        originalData.StageClears.Add(new StageClearData
        {
            StageIndex = 2,
            IsCleared = true,
            BestStars = 3
        });

        // 테스트 유저 데이터를 PlayerPrefs에 저장한다.
        //UserDataOperationResult saveResult =
        //    await service.SaveAsync(originalData);

        //// 저장에 실패했다면 이유를 출력하고 테스트를 중단한다.
        //if (!saveResult.Success)
        //{
        //    Debug.LogError($"저장 실패: {saveResult.ErrorMessage}");
        //    return;
        //}

        // 저장했던 유저 데이터를 PlayerPrefs에서 다시 불러온다.
        UserDataLoadResult loadResult =
            await service.LoadAsync();

        // 불러오기에 실패했다면 이유를 출력하고 테스트를 중단한다.
        if (!loadResult.Success)
        {
            Debug.LogError($"불러오기 실패: {loadResult.ErrorMessage}");
            return;
        }

        // 복원된 주요 값을 Console에 출력한다.
        Debug.Log(
            $"복원 완료: LastStage={loadResult.Data.LastClearedStageIndex}, " +
            $"StageIndex={loadResult.Data.StageClears[0].StageIndex}, " +
            $"Cleared={loadResult.Data.StageClears[0].IsCleared}, " +
            $"BestStars={loadResult.Data.StageClears[0].BestStars}");
    }
}
