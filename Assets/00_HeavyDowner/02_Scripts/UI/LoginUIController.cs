using HeavyDowner.Module;
using UnityEngine;

namespace HeavyDowner.UI
{
    public class LoginUIController : MonoBehaviour
    {
        [SerializeField] private InitializeManager initializer;

        private LoginUIView view;

        private void Awake()
        {
            TryGetComponent<LoginUIView>(out view);
        }

        private void OnEnable()
        {
            initializer.StepChanged += OnStepChanged;
            initializer.LoginRequired += OnLoginRequired;
            initializer.Completed += OnCompleted;
            view.GoogleLoginButton.onClick.AddListener(OnGoogleLoginClicked);
            view.GuestLoginButton.onClick.AddListener(OnGuestLoginClicked);
            view.ShowPreparing();
        }

        private void Start()
        {
            initializer.StartFlow();
        }

        private void OnDisable()
        {
            initializer.StepChanged -= OnStepChanged;
            initializer.LoginRequired -= OnLoginRequired;
            initializer.Completed -= OnCompleted;
            view.GoogleLoginButton.onClick.RemoveListener(OnGoogleLoginClicked);
            view.GuestLoginButton.onClick.RemoveListener(OnGuestLoginClicked);
        }

        private void OnStepChanged(string message)
        {
            view.SetMessage(message);
        }

        private void OnLoginRequired()
        {
            view.ShowLogin();
        }

        private void OnCompleted(bool success)
        {
            if (!success)
            {
                view.ShowLogin();
                PopupSingleton.Instance.ShowMessage("로그인에 실패했습니다.\r\n다시 시도해주세요.");
                return;
            }

            view.ShowCompleted();
            _ = LoadMainAsync();
        }

        private void OnGoogleLoginClicked()
        {
            view.ShowSigningIn();
            initializer.SelectLogin(LoginMethod.Google);
        }

        private void OnGuestLoginClicked()
        {
            view.ShowSigningIn();
            initializer.SelectLogin(LoginMethod.Guest);
        }

        private async Awaitable LoadMainAsync()
        {
            await LoadingBridgeService.LoadAsync(SceneType.Main, LoadingMode.Overlay);
        }
    }
}
