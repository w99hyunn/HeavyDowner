using System;
using HeavyDowner.Module;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace HeavyDowner.UI
{
    public sealed class LoginUIController : MonoBehaviour
    {
        [SerializeField] private InitializeManager initializer;

        private LoginUIView view;
        private bool isSigningIn;

        private void Awake()
        {
            TryGetComponent<LoginUIView>(out view);
        }

        private void OnEnable()
        {
            initializer.Initialized += OnInitialized;
            view.GoogleLoginButton.onClick.AddListener(OnGoogleLoginClicked);
            view.GuestLoginButton.onClick.AddListener(OnGuestLoginClicked);
            view.ShowPreparing();
        }

        private void OnDisable()
        {
            initializer.Initialized -= OnInitialized;
            view.GoogleLoginButton.onClick.RemoveListener(OnGoogleLoginClicked);
            view.GuestLoginButton.onClick.RemoveListener(OnGuestLoginClicked);
        }

        private void OnInitialized()
        {
            _ = PrepareAuthenticationAsync();
        }

        private void OnGoogleLoginClicked()
        {
            if (isSigningIn)
                return;

            _ = SignInWithGoogleAsync();
        }

        private void OnGuestLoginClicked()
        {
            if (isSigningIn)
                return;

            _ = SignInAsGuestAsync();
        }

        private async Awaitable PrepareAuthenticationAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();

                if (await TryRestoreSessionAsync())
                {
                    view.ShowCompleted();
                    await LoadMainAsync();
                    return;
                }

                view.ShowLogin();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                view.ShowLoginFailed();
            }
        }

        private async Awaitable<bool> TryRestoreSessionAsync()
        {
            if (!AuthenticationService.Instance.SessionTokenExists)
                return false;

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            return AuthenticationService.Instance.IsSignedIn;
        }

        private async Awaitable SignInAsGuestAsync()
        {
            isSigningIn = true;
            view.ShowGuestSigningIn();

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                view.ShowCompleted();
                await LoadMainAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                view.ShowLoginFailed();
            }
            finally
            {
                isSigningIn = false;
            }
        }

        private async Awaitable SignInWithGoogleAsync()
        {
            isSigningIn = true;
            view.ShowSigningIn();

            try
            {
                string idToken = await LoginHelper.GetIdTokenAsync();
                await AuthenticationService.Instance.SignInWithOpenIdConnectAsync(
                    LoginHelper.OIDC_PROVIDER_NAME,
                    idToken);
                view.ShowCompleted();
                await LoadMainAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                view.ShowLoginFailed();
            }
            finally
            {
                isSigningIn = false;
            }
        }

        private async Awaitable LoadMainAsync()
        {
            await LoadingBridge.LoadAsync(SceneType.Main, LoadingMode.Overlay);
        }
    }
}
