package com.pps.auth;

import android.app.Activity;
import android.os.CancellationSignal;

import androidx.annotation.NonNull;
import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialException;

import com.google.android.libraries.identity.googleid.GetGoogleIdOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;
import com.unity3d.player.UnityPlayer;

// Unity와 Android Google 로그인을 연결한다.
public final class GoogleCredentialBridge
{
    // 객체 생성을 막고 static 함수만 사용한다.
    private GoogleCredentialBridge()
    {
    }

    // Unity에서 호출하여 Google ID Token을 요청한다.
    public static void requestGoogleIdToken(
        String webClientId,
        String unityObjectName,
        String successMethod,
        String errorMethod)
    {
        // Google 로그인 UI를 표시할 Unity Activity를 가져온다.
        Activity activity = UnityPlayer.currentActivity;

        // Android UI 작업은 메인 스레드에서 실행해야 한다.
        activity.runOnUiThread(() ->
        {
            // Android Credential Manager를 생성한다.
            CredentialManager credentialManager =
                CredentialManager.create(activity);

            // Google 계정에서 ID Token을 요청하도록 설정한다.
            GetGoogleIdOption googleIdOption =
                new GetGoogleIdOption.Builder()
                    .setFilterByAuthorizedAccounts(false)
                    .setServerClientId(webClientId)
                    .build();

            // Google 로그인 옵션을 Credential 요청에 추가한다.
            GetCredentialRequest request =
                new GetCredentialRequest.Builder()
                    .addCredentialOption(googleIdOption)
                    .build();

            // Google 계정 선택 화면을 실행한다.
            credentialManager.getCredentialAsync(
                activity,
                request,
                new CancellationSignal(),
                activity.getMainExecutor(),
                new CredentialManagerCallback<
                    GetCredentialResponse,
                    GetCredentialException>()
                {
                    // Google 계정 선택에 성공했을 때 호출된다.
                    @Override
                    public void onResult(
                        @NonNull GetCredentialResponse result)
                    {
                        Credential credential = result.getCredential();

                        // 반환된 결과가 Google ID Token인지 확인한다.
                        if (credential instanceof CustomCredential
                            && GoogleIdTokenCredential
                                .TYPE_GOOGLE_ID_TOKEN_CREDENTIAL
                                .equals(credential.getType()))
                        {
                            try
                            {
                                // 반환 데이터를 Google 인증 정보로 변환한다.
                                GoogleIdTokenCredential googleCredential =
                                    GoogleIdTokenCredential.createFrom(
                                        credential.getData());

                                // ID Token을 Unity C# 함수로 전달한다.
                                UnityPlayer.UnitySendMessage(
                                    unityObjectName,
                                    successMethod,
                                    googleCredential.getIdToken());
                            }
                            catch (Exception exception)
                            {
                                sendError(
                                    unityObjectName,
                                    errorMethod,
                                    exception.getMessage());
                            }

                            return;
                        }

                        sendError(
                            unityObjectName,
                            errorMethod,
                            "Google 인증 정보가 아닙니다.");
                    }

                    // 계정 선택 취소 또는 요청 실패 시 호출된다.
                    @Override
                    public void onError(
                        @NonNull GetCredentialException exception)
                    {
                        sendError(
                            unityObjectName,
                            errorMethod,
                            exception.getMessage());
                    }
                });
        });
    }

    // 오류 내용을 Unity C# 함수로 전달한다.
    private static void sendError(
        String unityObjectName,
        String errorMethod,
        String message)
    {
        UnityPlayer.UnitySendMessage(
            unityObjectName,
            errorMethod,
            message == null ? "Google 로그인 실패" : message);
    }
}