using Cysharp.Threading.Tasks;
// Cysharp.Threading.Task : UniTask 반환
using Firebase.Auth;
// Auth : 현재 로그인 사용자의 UID 확인
using Firebase.Firestore;
// Firestore :  Firestore 문서 저장
using System;
// System : 저장 중 발생한 Exception 처리
using System.Collections.Generic;
// Collections.Generic : Firestore 문서 데이터 구성
using UnityEngine;
// UnityEngine : UserData를 JSON으로 변환

namespace PPS.Core
{
    // 로그인한 사용자의 UserData를 Cloud Firestore에 저장하고 불러온다.
    public class FirebaseUserDataStorage : IUserDataStorage
    {
        // 사용자 문서를 저장할 컬렉션 이름이다.
        const string UsersCollection = "users";

        // 로그인 성공 시 전달받은 사용자를 보관한다.
        readonly FirebaseUser _user;

        // 사용할 Firebase 사용자를 전달받는다.
        public FirebaseUserDataStorage(FirebaseUser user = null)
        {
            _user = user;
        }

        // 로그인한 사용자의 UserData를 저장한다.
        public async UniTask<UserDataOperationResult> SaveAsync(UserData data)
        {
            // 저장할 데이터가 없다면 요청하지 않는다.
            if (data == null)
            {
                return UserDataOperationResult.Failed("저장할 유저 데이터가 없습니다.");
            }
            // 현재 로그인한 사용자를 가져온다.
            FirebaseUser user = _user ?? FirebaseAuth.DefaultInstance.CurrentUser;

            // 사용자별 문서를 만들려면 로그인이 필요하다.
            if (user == null)
            {
                return UserDataOperationResult.Failed("로그인한 사용자가 없습니다.");
            }

            try

            {
                // 기존 UserData 구조를 그대로 보존한다.
                string json = JsonUtility.ToJson(data);

                // Firestore에 저장할 문서를 구성한다.
                var documentData = new Dictionary<string, object>
                {
                    {"version", data.Version }, {"payloadJson", json}, {"updatedAt", FieldValue.ServerTimestamp}

                };

                // users/{UID} 문서를 선택한다.
                DocumentReference document = FirebaseFirestore.DefaultInstance.Collection(UsersCollection).Document(user.UserId);

                // 같은 UID 문서에 유저 데이터를 저장한다.
                await document.SetAsync(documentData);

                // Firestore 저장 성공 결과를 반환한다.
                return UserDataOperationResult.Succeeded();
            }
            catch (Exception exception)
            {
                // Firebase 오류를 호출한 쪽에 전달한다.
                return UserDataOperationResult.Failed(exception.Message);
            }
        }
        // 로그인한 사용자의 UserData를 불러온다.
        public async UniTask<UserDataLoadResult> LoadAsync()
        {
            // 현재 로그인한 사용자를 가져온다.
            FirebaseUser user = _user ?? FirebaseAuth.DefaultInstance.CurrentUser;

            // 사용자별 문서를 찾으려면 로그인이 필요하다.
            if (user == null)
            {
                return UserDataLoadResult.Failed("NO_CURRENT_USER");
            }

            try
            {
                // users/{UID} 문서를 선택한다.
                DocumentReference document =FirebaseFirestore.DefaultInstance.
                    Collection(UsersCollection) .Document(user.UserId);
                    
                // 선택한 문서의 현재 내용을 불러온다.
                DocumentSnapshot snapshot = await document.GetSnapshotAsync();
                   

                // 아직 저장된 문서가 없는 사용자다.
                if (!snapshot.Exists)
                {
                    return UserDataLoadResult.Failed("DOCUMENT_NOT_FOUND");
                }

                // 문서에서 UserData JSON을 가져온다.
                if (!snapshot.TryGetValue("payloadJson",out string json))
                {
                    return UserDataLoadResult.Failed("PAYLOAD_JSON_MISSING");  
                }

                // 비어 있는 JSON은 복원할 수 없다.
                if (string.IsNullOrWhiteSpace(json))
                {
                    return UserDataLoadResult.Failed("PAYLOAD_JSON_EMPTY");  
                }

                // JSON을 기존 UserData 객체로 복원한다.
                UserData data =JsonUtility.FromJson<UserData>(json);
                  
                // JSON 형식이 잘못됐다면 실패 처리한다.
                if (data == null)
                {
                    return UserDataLoadResult.Failed("JSON_DESERIALIZE_FAILED");  
                }

                // 복원된 UserData와 성공 결과를 반환한다.
                return UserDataLoadResult.Succeeded(data);
            }
            catch (Exception exception)
            {
                // Firebase 오류를 호출한 쪽에 전달한다.
                return UserDataLoadResult.Failed(
                    exception.Message);
            }
        }
    }
}


