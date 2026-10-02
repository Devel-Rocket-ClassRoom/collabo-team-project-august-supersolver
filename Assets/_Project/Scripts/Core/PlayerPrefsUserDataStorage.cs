using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PPS.Core
{
    // UserData를 JSON으로 변환하여 PlayerPrefs에 저장하고 불러온다.
    public class PlayerPrefsUserDataStorage : IUserDataStorage
    {
        // PlayerPrefs에서 유저 데이터를 찾을 때 사용할 고정 키다.
        const string UserDataKey = "PPS.UserData";

        // 유저 데이터를 PlayerPrefs에 저장한다.
        public UniTask<UserDataOperationResult> SaveAsync(UserData data)
        {
            // 저장할 데이터가 없다면 실패 결과를 반환한다.
            if (data == null)
            {
                return UniTask.FromResult(
                    UserDataOperationResult.Failed("저장할 유저 데이터가 없습니다."));
            }

            try
            {
                // UserData 객체를 PlayerPrefs에 저장할 JSON 문자열로 변환한다.
                string json = JsonUtility.ToJson(data);

                // 지정한 키에 JSON 문자열을 저장한다.
                PlayerPrefs.SetString(UserDataKey, json);

                // 변경된 PlayerPrefs 내용을 실제 저장소에 반영한다.
                PlayerPrefs.Save();

                // 저장이 정상적으로 끝났음을 반환한다.
                return UniTask.FromResult(
                    UserDataOperationResult.Succeeded());
            }
            catch (Exception exception)
            {
                // 저장 과정에서 예외가 발생하면 실패 이유를 반환한다.
                return UniTask.FromResult(
                    UserDataOperationResult.Failed(exception.Message));
            }
        }

        // PlayerPrefs에 저장된 유저 데이터를 불러온다.
        public UniTask<UserDataLoadResult> LoadAsync()
        {
            // 저장된 키가 없다면 불러올 데이터가 없는 상태다.
            if (!PlayerPrefs.HasKey(UserDataKey))
            {
                return UniTask.FromResult(
                    UserDataLoadResult.Failed("저장된 유저 데이터가 없습니다."));
            }

            try
            {
                // PlayerPrefs에서 저장된 JSON 문자열을 가져온다.
                string json = PlayerPrefs.GetString(UserDataKey);

                // JSON 문자열을 다시 UserData 객체로 복원한다.
                UserData data = JsonUtility.FromJson<UserData>(json);

                // JSON을 유저 데이터로 복원하지 못했다면 실패 처리한다.
                if (data == null)
                {
                    return UniTask.FromResult(
                        UserDataLoadResult.Failed("유저 데이터를 복원하지 못했습니다."));
                }

                // 복원된 유저 데이터와 함께 성공 결과를 반환한다.
                return UniTask.FromResult(
                    UserDataLoadResult.Succeeded(data));
            }
            catch (Exception exception)
            {
                // 불러오기 과정에서 예외가 발생하면 실패 이유를 반환한다.
                return UniTask.FromResult(
                    UserDataLoadResult.Failed(exception.Message));
            }
        }
    }
}