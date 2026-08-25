using UnityEngine;
using Firebase.Auth;
using System;

namespace PPS.Core
{
    // Firebase 로그인 성공 후 해당 사용자의 UserData를 불러온다
    public class FirebaseUserDataLoader : MonoBehaviour
    {
        // 로그인 성공 이벤트를 받을 인증 서비스다.
        [SerializeField] FirebaseAuthService _authService;

        // 현재 불러온 유저 데이터를 보관한다.
        public UserData CurrentData { get; private set; }

        // UserData 불러오기 또는 신규 생성이 완료됐을 때 외부에 알린다.
        public event Action<UserData> DataLoaded;

        // UserData 처리에 실패했을 때 오류 내용을 외부에 알린다.
        public event Action<string> DataLoadFailed;

        void OnEnable()
        {
            // 인증 서비스가 연결되어 있다면 로그인 성공 이벤트를 구독한다.
            if (_authService != null)
                _authService.SignedIn += OnSignedIn;
        }
        void OnDisable()
        {
            // 오브젝트가 꺼질 때 이벤트 구독을 해제한다.
            if (_authService != null)
                _authService.SignedIn -= OnSignedIn;
        }

        // 외부에서 현재 로그인한 사용자의 데이터를 불러오도록 요청한다.
        public void LoadCurrentUserData()
        {
            // Firebase 초기화가 완료되지 않았거나 로그인 사용자가 없다면 불러올 수 없다.
            if (_authService == null || _authService.CurrentUser == null)
            {
                Debug.LogWarning("UserData를 불러올 로그인 사용자가 없습니다.");
                return;
            }
            // 기존에 작성한 로그인 후 데이터 불러오기 로직을 재사용한다.
            OnSignedIn(_authService.CurrentUser);
        }
        async void OnSignedIn (FirebaseUser user)
        {
            // Firebase용 저장 구현체를 만든다.
            IUserDataStorage storage = new FirebaseUserDataStorage(user);

            // 공용 Save/Load 흐름을 담당하는 서비스를 만든다.
            UserDataService service = new UserDataService(storage);

            Debug.Log($"Google 사용자 데이터 불러오기 시작 UID = {user.UserId}");

            // users / {현재 로그인 UID } 문서를 불러온다.
            UserDataLoadResult result = await service.LoadAsync();

            // 불러오기에 실패했는지 확인한다.
            if (!result.Success)
            {
                // 문서가 없어서 실패한 것이 아니라면 실제 오류이므로 중단한다.
                if (result.ErrorMessage != "DOCUMENT_NOT_FOUND")
                {
                    Debug.LogWarning(
                        $"Google 사용자 데이터 불러오기 실패: {result.ErrorMessage}");

                    // 외부에 불러오기 실패 사실과 원인을 전달한다.
                    DataLoadFailed ?.Invoke(result.ErrorMessage);

                    return;
                }

                // 문서가 없다는 것은 이 구글 계정으로 처음 접속했다는 뜻이다.
                // UserData의 기본값으로 신규 사용자 데이터를 생성한다.
                UserData newUserData = new UserData();

                // 신규 사용자 데이터를 현재 구글 계정의 UID 문서에 저장한다.
                UserDataOperationResult saveResult =
                    await service.SaveAsync(newUserData);

                // 신규 데이터 저장에 실패했다면 이후 처리를 중단한다.
                if (!saveResult.Success)
                {
                    Debug.LogError(
                        $"신규 Google 사용자 데이터 저장 실패: {saveResult.ErrorMessage}");

                    // 외부에 신규 데이터 저장 실패 사실을 전달한다.
                    DataLoadFailed ?. Invoke(saveResult.ErrorMessage);

                    return;
                }

                // 새로 생성한 데이터를 현재 게임에서 사용할 데이터로 보관한다.
                CurrentData = newUserData;

                Debug.Log(
                    $"신규 Google 사용자 데이터 생성 완료: " +
                    $"UID={user.UserId}, " +
                    $"LastClearedStageIndex={CurrentData.LastClearedStageIndex}");
                // 신규 UserData 준비가 끝났다고 외부에 알린다.
                DataLoaded?.Invoke(CurrentData);

                return;
            }
            // 불러온 데이터를 현재 데이터로 보관한다.
            CurrentData = result.Data;

            // 주요 값을 확인한다.
            Debug.Log($"Google 사용자 데이터 불러오기 완료:" + $"LastClearedStageIndex = {CurrentData.LastClearedStageIndex}");

            // 기존 UserData를 정상적으로 불러왔다고 외부에 알린다.
            DataLoaded?.Invoke(CurrentData );
        }
    }
}

