using System;
using Cysharp.Threading.Tasks;

namespace PPS.Core
{
    public interface IUserDataService
    {
        event Action BeforeSave;
        event Action<UserDataOperationResult> AfterSave;
        event Action BeforeLoad;
        event Action<UserDataLoadResult> AfterLoad;
        UniTask<UserDataOperationResult> SaveAsync(UserData data);
        UniTask<UserDataLoadResult> LoadAsync();
    }
    // 저장 방식과 게임 코드 사이에서 Save/Load 흐름과 이벤트를 관리한다.
    public class UserDataService : IUserDataService
    {
        // 실제 저장과 불러오기를 수행할 구현체를 보관한다.
        // 다음 단계에서 PlayerPrefs 구현체가 들어간다.
        readonly IUserDataStorage _storage;

        // 유저 데이터 저장을 시작하기 직전에 발생한다.
        public event Action BeforeSave;

        // 유저 데이터 저장이 끝난 후 결과와 함께 발생한다.
        public event Action<UserDataOperationResult> AfterSave;

        // 유저 데이터 불러오기를 시작하기 직전에 발생한다.
        public event Action BeforeLoad;

        // 유저 데이터 불러오기가 끝난 후 결과와 함께 발생한다.
        public event Action<UserDataLoadResult> AfterLoad;

        // 외부에서 사용할 저장 구현체를 전달받는다.
        public UserDataService(IUserDataStorage storage)
        {
            // 저장 구현체 없이 서비스를 만들면 사용할 수 없으므로 즉시 막는다.
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        // 유저 데이터를 비동기로 저장한다.
        public async UniTask<UserDataOperationResult> SaveAsync(UserData data)
        {
            // 실제 저장을 시작하기 전에 구독자에게 알린다.
            BeforeSave?.Invoke();

            // 연결된 저장 구현체에 유저 데이터 저장을 요청한다.
            UserDataOperationResult result = await _storage.SaveAsync(data);

            // 저장이 끝난 후 성공 또는 실패 결과를 구독자에게 전달한다.
            AfterSave?.Invoke(result);

            // SaveAsync를 호출한 쪽에도 동일한 결과를 반환한다.
            return result;
        }

        // 저장된 유저 데이터를 비동기로 불러온다.
        public async UniTask<UserDataLoadResult> LoadAsync()
        {
            // 실제 불러오기를 시작하기 전에 구독자에게 알린다.
            BeforeLoad?.Invoke();

            // 연결된 저장 구현체에 유저 데이터 불러오기를 요청한다.
            UserDataLoadResult result = await _storage.LoadAsync();

            // 불러오기가 끝난 후 결과와 복원된 데이터를 구독자에게 전달한다.
            AfterLoad?.Invoke(result);

            // LoadAsync를 호출한 쪽에도 동일한 결과를 반환한다.
            return result;
        }
    }
}