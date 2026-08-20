using UnityEngine;

namespace PPS.Core
{
    // Android의 Google 계정 선택 기능과 Firebase 로그인을 연결한다.
    public class GoogleSignInBridge : MonoBehaviour
    {
        // Google ID Token을 전달할 Firebase 인증 서비스를 연결한다.
        [SerializeField] FirebaseAuthService _authService;

        // Google 로그인 요청에 사용할 Web OAuth 클라이언트 ID다.
        [SerializeField] string _webClientId;

        // Google 로그인을 시작한다.
        // 나중에 Google 로그인 버튼이 이 함수를 호출한다.
        public void StartGoogleSignIn()
        {
            // Firebase 인증 서비스가 Inspector에 연결됐는지 확인한다.
            if (_authService == null)
            {
                Debug.LogError("FirebaseAuthService가 연결되지 않았습니다.");
                return;
            }

            // Google 로그인 요청에 필요한 Web Client ID가 입력됐는지 확인한다.
            if (string.IsNullOrWhiteSpace(_webClientId))
            {
                Debug.LogError("Google Web Client ID가 입력되지 않았습니다.");
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // Android 실제 기기에서는 Google 계정 선택을 요청한다.
            RequestGoogleCredential();
#else
            // Credential Manager는 Unity Editor에서 실제 계정 선택을 실행할 수 없다.
            Debug.LogWarning("Google 로그인은 Android 빌드에서 확인해야 합니다.");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Java 브리지에 Google 계정 선택을 요청한다.
        void RequestGoogleCredential()
        {
            // 작성한 Java 브리지 클래스를 찾는다.
            using AndroidJavaClass bridge = new("com.pps.auth.GoogleCredentialBridge");

            // Java에 계정 선택과 ID Token 발급을 요청한다
            bridge.CallStatic("requestGoogleIdToken", _webClientId, gameObject.name,
            nameof(OnGoogleIdTokenReceived), nameof(OnGoogleSignInError));
        }
#endif

        // Java에서 Google ID Token을 받아면 호출된다.
        public void OnGoogleIdTokenReceived(string idToken)
        {
            // 빈 토큰은 Firebase에 전달하지 않는다.
            if (string.IsNullOrWhiteSpace(idToken))
            {
                Debug.LogError("Google ID Token이 비어 있습니다.");
                return;
            }
            Debug.Log("Google ID Token 수신 완료");

            // 받은 토큰으로 Firebase 로그인을 요청한다.
            _authService.SignInWithGoogle(idToken);
        }

        // Java의 Google 로그인 요청이 실패하면 호출된다.
        public void OnGoogleSignInError(string message)
        {
            Debug.LogError($"Google 계정 선택 실패: {message}");
        }
    } // GoogleSignInBridge 끝
} // namespace 끝

