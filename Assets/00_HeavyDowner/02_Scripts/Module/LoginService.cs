using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Scripting;

namespace HeavyDowner.Module
{
    public static class LoginService
    {
        private const string GOOGLE_WEB_CLIENT_ID = "1005757902027-qbnulidorjvf02tmtchf8jqulg32u3b0.apps.googleusercontent.com";

        private static GoogleSignInCallback callback;

        public static bool HasSession => AuthenticationService.Instance.SessionTokenExists;

        public static async Awaitable InitializeAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
        }

        public static async Awaitable RestoreAsync()
        {
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public static async Awaitable SignInGoogleAsync()
        {
            string idToken = await GetIdTokenAsync();
            await AuthenticationService.Instance.SignInWithGoogleAsync(idToken);
        }

        public static async Awaitable SignInGuestAsync()
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        private static async Awaitable<string> GetIdTokenAsync()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (callback != null)
                throw new InvalidOperationException("Google sign-in is already in progress.");

            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var currentCallback = new GoogleSignInCallback(completion);
            callback = currentCallback;

            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var credentialManager = new AndroidJavaClass("com.heavydowner.auth.GoogleCredentialManagerBridge");
                credentialManager.CallStatic("signIn", activity, GOOGLE_WEB_CLIENT_ID, currentCallback);

                string idToken = await completion.Task;
                await Awaitable.MainThreadAsync();
                return idToken;
            }
            finally
            {
                if (ReferenceEquals(callback, currentCallback))
                    callback = null;
            }
#else
            await Awaitable.NextFrameAsync();
            throw new PlatformNotSupportedException("Google sign-in is only available in an Android device build.");
#endif
        }

        [Preserve]
        private class GoogleSignInCallback : AndroidJavaProxy
        {
            private readonly TaskCompletionSource<string> completion;

            public GoogleSignInCallback(TaskCompletionSource<string> completion)
                : base("com.heavydowner.auth.GoogleCredentialManagerBridge$SignInCallback")
            {
                this.completion = completion;
            }

            [Preserve]
            public void onSuccess(string idToken)
            {
                completion.TrySetResult(idToken);
            }

            [Preserve]
            public void onError(string message)
            {
                completion.TrySetException(new InvalidOperationException(message));
            }
        }
    }
}
