
namespace PPS.Core
{
    // 유저 데이터 저장 작업의 성공 여부와 실패 이유를 전달한다.
    public class UserDataOperationResult
    {
        // 작업이 성공했는지 나타낸다.
        public bool Success;

        // 작업에 실패한 이유를 저장한다.
        // 성공했을 때는 빈 문자열을 사용한다.
        public string ErrorMessage;

        // 저장 성공 결과를 생성한다.
        public static UserDataOperationResult Succeeded()
        {
            return new UserDataOperationResult
            {
                Success = true,
                ErrorMessage = string.Empty
            };
        }

        // 저장 실패 결과와 실패 이유를 생성한다.
        public static UserDataOperationResult Failed(string errorMessage)
        {
            return new UserDataOperationResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    // 유저 데이터 불러오기 결과를 전달한다.
    public class UserDataLoadResult
    {
        // 불러오기가 성공했는지 나타낸다.
        public bool Success;

        // 불러오기에 실패한 이유를 저장한다.
        public string ErrorMessage;

        // 불러오기에 성공했을 때 복원된 유저 데이터를 저장한다.
        public UserData Data;

        // 불러오기 성공 결과와 복원된 데이터를 생성한다.
        public static UserDataLoadResult Succeeded(UserData data)
        {
            return new UserDataLoadResult
            {
                Success = true,
                ErrorMessage = string.Empty,
                Data = data
            };
        }

        // 불러오기 실패 결과와 실패 이유를 생성한다.
        public static UserDataLoadResult Failed(string errorMessage)
        {
            return new UserDataLoadResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Data = null
            };
        }
    }
}