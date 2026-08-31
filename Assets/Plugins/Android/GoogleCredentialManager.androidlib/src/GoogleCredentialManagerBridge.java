package com.heavydowner.auth;

import android.app.Activity;

import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialException;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

import java.util.concurrent.Executor;

public final class GoogleCredentialManagerBridge {
    public interface SignInCallback {
        void onSuccess(String idToken);

        void onError(String message);
    }

    private GoogleCredentialManagerBridge() {
    }

    public static void signIn(
            Activity activity,
            String webClientId,
            SignInCallback callback) {
        GetSignInWithGoogleOption googleOption = new GetSignInWithGoogleOption.Builder(webClientId)
                .build();
        GetCredentialRequest request = new GetCredentialRequest.Builder()
                .addCredentialOption(googleOption)
                .build();
        CredentialManager credentialManager = CredentialManager.create(activity);
        Executor mainExecutor = command -> activity.runOnUiThread(command);

        credentialManager.getCredentialAsync(
                activity,
                request,
                null,
                mainExecutor,
                new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                    @Override
                    public void onResult(GetCredentialResponse result) {
                        Credential credential = result.getCredential();
                        if (!(credential instanceof CustomCredential)) {
                            callback.onError("Unexpected credential type.");
                            return;
                        }

                        CustomCredential customCredential = (CustomCredential) credential;
                        if (!GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(customCredential.getType())) {
                            callback.onError("Unexpected Google credential type.");
                            return;
                        }

                        try {
                            GoogleIdTokenCredential googleCredential =
                                    GoogleIdTokenCredential.createFrom(customCredential.getData());
                            callback.onSuccess(googleCredential.getIdToken());
                        } catch (RuntimeException exception) {
                            callback.onError(exception.getMessage());
                        }
                    }

                    @Override
                    public void onError(GetCredentialException exception) {
                        callback.onError(exception.getClass().getSimpleName() + ": " + exception.getMessage());
                    }
                });
    }
}
