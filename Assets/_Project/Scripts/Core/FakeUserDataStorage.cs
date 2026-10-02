using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PPS.Core
{
    public class FakeUserDataStorage : IUserDataStorage
    {
        private UserData fakeData;
        private readonly string _errorMessage = "가짜 데이터인데 에러가 왜나요";
        public FakeUserDataStorage()
        {
            fakeData = new UserData();

            // 1-18 까지 깬 계정. 잠금 판정이 HasPlayed 가
            // false 면 LastCleared 를 보지 않으므로 같이 켠다.
            fakeData.HasPlayed = true;
            fakeData.LastCleared = new StageEntry(0, 17);
        }
        public UniTask<UserDataLoadResult> LoadAsync()
        {
            UserDataLoadResult fakeResult = new UserDataLoadResult()
            {
                Data = fakeData,
                ErrorMessage = _errorMessage,
                Success = true,
            };
            return UniTask.FromResult<UserDataLoadResult>(fakeResult);
        }

        public UniTask<UserDataOperationResult> SaveAsync(UserData data)
        {
            fakeData = data;
            UserDataOperationResult fakeResult = new UserDataOperationResult()
            {
                ErrorMessage = _errorMessage,
                Success = true
            };
            return UniTask.FromResult<UserDataOperationResult>(fakeResult);
        }
    }
}
