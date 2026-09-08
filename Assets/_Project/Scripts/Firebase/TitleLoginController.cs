using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace PPS.Core
{
    public class TitleLoginController : MonoBehaviour
    {
        //현재 Firebase 로그인 상태를 확인한다.
        [SerializeField] FirebaseAuthService _authService;

        // Android 에서 Google 계정 선택을 실행한다.
        [SerializeField] GoogleSignInBridge _googleSignInBridge;

        // 로그인한 사용자의 UserData를 불러온다
        [SerializeField] FirebaseUserDataLoader _userDataLoader;

        // Unity Editor에서 개발자 로그인 화면을 표시한다.
        [SerializeField] GameObject _firebaseLoginPanel;

        // 로그인 또는 사용자 데이터 준비 실패를 플레이어에게 안내
        [SerializeField] TextMeshProUGUI _loginResultText;
        void OnEnable()
        {
            // UserData 로드 결과를 받을 함수를 연결한다.
            _userDataLoader.DataLoaded += OnUserDataLoaded;

            // UserData 로드 실패 결과를 받을 함수를 연결한다.
            _userDataLoader.DataLoadFailed += OnUserDataLoadFailed;
        }
        void OnDisable()
        {
            // 오브젝트가 비활성화 될 때 이벤트 연결을 해제한다.
            _userDataLoader.DataLoaded -= OnUserDataLoaded;

            // 중복 호출을 방지하기 위해 실패 이벤트도 해제한다.
            _userDataLoader.DataLoadFailed -= OnUserDataLoadFailed;
        }

        public void StartGame()
        {
            // Firebase 초기화가 아직 끝나지 않았다면 이번 요청은 중단한다
            if (!_authService.IsReady)
            {
                return;
            }
            {
                // 이전 로그인 기록이 남아 있다면 Google 계정 선택을 다시 띄우지 않는다.
                if (_authService.CurrentUser != null)
                {
                    // 로그인된 사용자의 Firebase UserData를 불러온다.
                    // 성공하면 기존 OnUserDataLoaded()가 게임씬으로 이동시킨다.
                    _userDataLoader.LoadCurrentUserData();
                    return;
                }
                // 로그인 기록이 없다면 기존 Google 로그인 절차를 시작한다.
                _googleSignInBridge.StartGoogleSignIn();
            }
        }
        void OnUserDataLoaded(UserData userData)
        {
            // 이전 시도에서 표시된 실패 안내를 제거
            if (_loginResultText != null)
            {
                _loginResultText.text = string.Empty;
            }
            // 불러온 진행 정보를 Console에 출력
            Debug.Log($"게임 시작 준비 완료: LastClearedStageIndex = {userData.LastClearedStageIndex}");

            // userData 준비가 끝났으므로 스테이지 선택 씬으로 이동한다.
            SceneManager.LoadScene("StageSelect");
        }

        private void OnUserDataLoadFailed(string errorMessage)
        {
            // 데이터 준비에 실패했다면 씬을 이동하지 않고 원인을 출력한다.
            Debug.LogError($"게임 시작 실패: {errorMessage}");

            // 플레이어에게 이해하기 쉬운 실패 안내를 표시
            if (_loginResultText != null)
            {
                _loginResultText.text = " 로그인 정보를 불러오지 못했습니다. /n 잠시 후 시도해주세요.";
            }
        }

        // 앱을 다시 실행했을 때 기존 Firebase로그인 세션을 확인한다.
    }
}

