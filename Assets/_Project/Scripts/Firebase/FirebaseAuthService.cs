using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

namespace PPS.Core
{
    public class FirebaseAuthService : MonoBehaviour
    {
        // Firebase의 회원가입·로그인·로그아웃을 담당하는 객체다.
        FirebaseAuth _auth;

        // Firebase 초기화 완료 여부를 외부에서 확인할 수 있게 한다.
        public bool IsReady { get; private set; }

        // 현재 로그인된 사용자를 외부에서 확인할 수 있게 한다.
        public FirebaseUser CurrentUser => _auth?.CurrentUser;

        void Start()
        {
            // 오브젝트가 시작되면 Firebase 연결 상태를 확인한다.
            InitializeFirebase();
        }

        void InitializeFirebase()
        {
            // Firebase SDK에 필요한 의존성이 정상적으로 설치됐는지 확인하고 보정한다.
            FirebaseApp.CheckAndFixDependenciesAsync()
                .ContinueWithOnMainThread(task =>
                {
                    // 의존성 확인 작업이 취소됐는지 검사한다.
                    if (task.IsCanceled)
                    {
                        Debug.LogError("Firebase 초기화 작업이 취소되었습니다.");
                        return;
                    }

                    // 의존성 확인 과정에서 오류가 발생했는지 검사한다.
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"Firebase 초기화 오류: {task.Exception}");
                        return;
                    }

                    // Firebase를 사용할 수 있는 상태가 아니면 초기화를 중단한다.
                    if (task.Result != DependencyStatus.Available)
                    {
                        Debug.LogError($"Firebase 의존성 오류: {task.Result}");
                        return;
                    }

                    // 현재 Firebase 프로젝트의 Authentication 객체를 가져온다.
                    _auth = FirebaseAuth.DefaultInstance;

                    // 회원가입과 로그인 함수를 사용할 수 있는 상태로 표시한다.
                    IsReady = true;

                    Debug.Log("Firebase Authentication 초기화 완료");

                    // 이전 로그인 정보가 남아 있는지 확인한다.
                    if (_auth.CurrentUser == null)
                    {
                        Debug.Log("현재 로그인된 사용자가 없습니다.");
                    }
                    else
                    {
                        Debug.Log($"로그인 유지 확인: UID={_auth.CurrentUser.UserId}");
                    }
                });
        }

        public void SignInWithGoogle(string googleIdToken)
        {
            // Google에서 받은 ID Token을 Firebase 인증 정보로 바꾼다.
            Credential credential = GoogleAuthProvider.GetCredential(googleIdToken, null);

            // 변환한 인증 정보로 Firebase에 로그인한다.
            _auth.SignInWithCredentialAsync(credential);
        }

        // 입력 받은 이메일과 비밀번호로 새로운 Firebase 계정을 만든다.
        public void SignUp(string email, string password)
        {
            // Firebase 초기화가 끝나지 않았다면 회원가입을 실행하지 않는다.
            if (!IsReady)
            {
                Debug.LogWarning("Firebase 초기화가 아직 완료되지 않았습니다.");
                return;
            }

            // 이메일이나 비밀번호가 비어 이다면 요청하지 않는다.
            if(string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                Debug.LogWarning("이메일과 비밀번호를 모두 입력해야 합니다");
                return;
            }
            // Firebase에 이메일과 비밀번호를 전달하여 계정을 생성한다.
            _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                // 회원가입 작업이 취소 됐는지 확인한다.
                if (task.IsCanceled)
                {
                    Debug.LogWarning("Firebase 회원가입이 취소되었습니다.");
                    return;
                }

                // 이메일 중복이나 비밀번호 조건 등으로 실패했는지 확인한다.
                if (task.IsFaulted)
                {
                    Debug.LogError($"Firebase 회원가입 실패: {task.Exception}");
                    return;
                }
                // 생성된 Firebase 사용자를 가져온다.
                FirebaseUser user = task.Result.User;

                Debug.Log($"Firebase 회원가입 완료: Email = {user.Email}, UID = {user.UserId}");
            });
        }

        // 기존 Firebase 계정으로 로그인한다.
        public void SignIn(string email, string password)
        {
            //Firebase 초기화가 완료되지 않았다면 로그인 하지 않는다.
            if (!IsReady)
            {
                Debug.LogWarning("Firebase 초기화가 아직 완료되지 않았습니다.");
                return;
            }

            // 이메일과 비밀번호가 모두 입력됐는지 확인한다.
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                Debug.LogWarning("이메일과 비밀번호를 모두 입력해야 합니다.");
                return;
            }

            // 입력받은 이메일과 비밀번호로 Firebase 로그인을 요청한다.
            _auth.SignInWithEmailAndPasswordAsync(email, password)
                .ContinueWithOnMainThread(task =>
                {
                    // 로그인 작업이 취소됐는지 확인한다.
                    if (task.IsCanceled)
                    {
                        Debug.LogWarning("Firebase 로그인이 취소되었습니다.");
                        return;
                    }

                    // 잘못된 이메일이나 비밀번호 등으로 실패했는지 확인한다.
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"Firebase 로그인 실패: {task.Exception}");
                        return;
                    }

                    // 로그인된 Firebase 사용자 정보를 가져온다.
                    FirebaseUser user = task.Result.User;

                    Debug.Log(
                        $"Firebase 로그인 완료: Email={user.Email}, UID={user.UserId}");
                });
        }
        // 헌재 로그인된 Firebase 계정에서 로그아웃 된다.
        public void SignOut()
        {
            // Firebase 초기화가 완료되지 않았다면 실행하지 않는다.
            if (!IsReady)
            {
                Debug.LogWarning("Firebase 초기화가 아직 완료되지 않았습니다.");
                return;
            }
            // 현재 계정에서 로그아웃 한다.
            _auth.SignOut();

            Debug.Log("Firebase 로그아웃 완료");
        }
    }
}