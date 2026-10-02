using Cysharp.Threading.Tasks;

namespace PPS.Core
{
    // 유저 데이터를 저장하고 불러오는 저장 방식의 공통 규칙이다.
    // PlayerPrefs와 Firebase 저장 방식은 이 인터페이스를 각각 구현한다.
    public interface IUserDataStorage
    {
        // 유저 데이터를 비동기로 저장하고 작업 결과를 반환한다.
        UniTask<UserDataOperationResult> SaveAsync(UserData data);

        // 유저 데이터를 비동기로 불러오고 작업 결과를 반환한다.
        UniTask<UserDataLoadResult> LoadAsync();
    }
}