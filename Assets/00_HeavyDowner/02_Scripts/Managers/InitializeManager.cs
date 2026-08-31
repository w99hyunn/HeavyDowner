using System;
using UnityEngine;

namespace HeavyDowner.Module
{
    public enum LoginMethod
    {
        None,
        Google,
        Guest
    }

    public sealed class InitializeManager : MonoBehaviour
    {
        public event Action<string> StepChanged;
        public event Action LoginRequired;
        public event Action<bool> Completed;

        private LoginMethod loginMethod;

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        public void StartFlow()
        {
            _ = StartFlowAsync();
        }

        public void SelectLogin(LoginMethod method)
        {
            loginMethod = method;
        }

        private async Awaitable StartFlowAsync()
        {
            try
            {
                SetStep("서비스 초기화 중..");
                await LoginService.InitializeAsync();

                SetStep("로그인 확인 중..");
                if (LoginService.HasSession)
                {
                    await LoginService.RestoreAsync();
                }
                else
                {
                    SetStep("");
                    LoginRequired?.Invoke();

                    while (loginMethod == LoginMethod.None)
                        await Awaitable.NextFrameAsync(destroyCancellationToken);

                    SetStep("로그인 중..");
                    if (loginMethod == LoginMethod.Google)
                        await LoginService.SignInGoogleAsync();
                    else
                        await LoginService.SignInGuestAsync();
                }

                SetStep("데이터 불러오는 중..");
                await PlayerDataService.LoadAsync();

                SetStep("로그인 완료");
                Completed?.Invoke(true);
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private void SetStep(string message)
        {
            StepChanged?.Invoke(message);
        }

        private void Fail(Exception exception)
        {
            Debug.LogException(exception);
            Completed?.Invoke(false);
        }
    }
}
