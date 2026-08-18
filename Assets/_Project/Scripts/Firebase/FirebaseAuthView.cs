using UnityEngine;
using TMPro;

namespace PPS.Core
{
    public class FirebaseAuthView : MonoBehaviour
    {
        // 실제 Firebase 회원가입과 로그인을 처리하는 서비스다.
        [SerializeField] FirebaseAuthService _authService;

        // 사용자가 이메일을 입력하는 입력창이다.
        [SerializeField] TMP_InputField _emailInput;

        // 사용자가 비밀번호를 입력하는 입력 창이다.
        [SerializeField] TMP_InputField _passwordInput;

        // 회원가입 버튼에서 호출하는 함수다.
        public void OnSignUpClicked()
        {
            // Inspector 연결이 빠졌다면 회원가입을 실행하지 않는다.
            if (_authService == null || _emailInput == null || _passwordInput == null)
            {
                Debug.LogError("Firebase 회원가입 UI 연결을 확인해야 합니다.");
                return;
            }
            // 이메일 앞뒤에 실수로 들어간 공백을 제거한다.
            string email = _emailInput.text.Trim();

            // 비밀번호는 입력한 원문을 그대로 사용한다.
            string password = _passwordInput.text;

            // FirebaseAuthService에 회원가입을 요청한다.
            _authService.SignUp(email, password);
        }

        // 로그인 버튼을 눌렀을 때 실행한다.
        public void OnSignInClicked()
        {
            Debug.Log("로그인 버튼 클릭 확인");
            // Inspector에서 필요한 참조가 모두 연결됐는지 확인한다.
            if (_authService == null ||
                _emailInput == null ||
                _passwordInput == null)
            {
                Debug.LogError("Firebase 로그인 UI 연결을 확인해야 합니다.");
                return;
            }

            // 이메일 앞뒤의 불필요한 공백을 제거한다.
            string email = _emailInput.text.Trim();

            // 비밀번호는 입력한 값을 그대로 가져온다.
            string password = _passwordInput.text;

            // FirebaseAuthService에 로그인을 요청한다.
            _authService.SignIn(email, password);
        }

        public void OnSignOutClicked()
        {
            // FirebaseAuthService가 연결됐는지 확인한다.
            if (_authService == null)
            {
                Debug.LogError("FirebaseAuthService 연결을 확인해야 합니다.");
                return;
            }
            // 현재 Firebase 계정에서 로그아웃한다.
            _authService.SignOut();
        }
    }

}

