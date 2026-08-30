using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HeavyDowner.Module
{
    public static class LoginHelper
    {
        public const string OIDC_PROVIDER_NAME = "oidc-google";

        private const string GOOGLE_WEB_CLIENT_ID = "1005757902027-qbnulidorjvf02tmtchf8jqulg32u3b0.apps.googleusercontent.com";
        private const string CALLBACK_OBJECT_NAME = "GoogleLoginCallback";
        private const string CALLBACK_METHOD_NAME = nameof(GoogleLoginCallback.OnGoogleSignInResult);
        private const string RESULT_OK_PREFIX = "OK:";
        private const string RESULT_ERROR_PREFIX = "ERROR:";

        private static TaskCompletionSource<string> completion;
        private static GoogleLoginCallback callback;

        public static async Awaitable<string> GetIdTokenAsync()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (completion != null)
                throw new InvalidOperationException("Google sign-in is already in progress.");

            EnsureCallback();
            completion = new TaskCompletionSource<string>();

            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var credentialManager = new AndroidJavaClass("com.heavydowner.auth.GoogleCredentialManagerBridge");
            credentialManager.CallStatic(
                "signIn",
                activity,
                GOOGLE_WEB_CLIENT_ID,
                CALLBACK_OBJECT_NAME,
                CALLBACK_METHOD_NAME);

            return await completion.Task;
#else
            await Awaitable.NextFrameAsync();
            throw new PlatformNotSupportedException("Credential Manager Google sign-in is only available in an Android device build.");
#endif
        }

        private static void EnsureCallback()
        {
            if (callback != null)
                return;

            var callbackObject = new GameObject(CALLBACK_OBJECT_NAME);
            UnityEngine.Object.DontDestroyOnLoad(callbackObject);
            callback = callbackObject.AddComponent<GoogleLoginCallback>();
        }

        private static void Complete(string payload)
        {
            var currentCompletion = completion;
            completion = null;

            if (currentCompletion == null)
                return;

            if (payload.StartsWith(RESULT_OK_PREFIX, StringComparison.Ordinal))
            {
                currentCompletion.TrySetResult(payload.Substring(RESULT_OK_PREFIX.Length));
                return;
            }

            string message = payload.StartsWith(RESULT_ERROR_PREFIX, StringComparison.Ordinal)
                ? payload.Substring(RESULT_ERROR_PREFIX.Length)
                : "Google sign-in failed.";
            currentCompletion.TrySetException(new InvalidOperationException(message));
        }

        private sealed class GoogleLoginCallback : MonoBehaviour
        {
            public void OnGoogleSignInResult(string payload)
            {
                Complete(payload ?? string.Empty);
            }
        }
    }
}
