using HeavyDowner.Module;
using UnityEngine;

namespace HeavyDowner.UI
{
    public sealed class LoginUIController : MonoBehaviour
    {
        [SerializeField] private InitializeHelper initializer;

        private LoginUIView view;

        private void Awake()
        {
            TryGetComponent<LoginUIView>(out view);
        }

        private void OnEnable()
        {
            initializer.Initialized += OnInitialized;
            view.ShowLoading();
        }

        private void OnDisable()
        {
            initializer.Initialized -= OnInitialized;
        }

        private void OnInitialized()
        {
            view.ShowCompleted();
            _ = LoadMainAsync();
        }

        private async Awaitable LoadMainAsync()
        {
            await LoadingBridge.LoadAsync(SceneType.Main, LoadingMode.Overlay);
        }
    }
}
