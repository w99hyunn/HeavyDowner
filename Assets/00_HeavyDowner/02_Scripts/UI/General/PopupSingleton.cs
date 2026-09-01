using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeavyDowner.UI
{
    [DefaultExecutionOrder(-1000)]
    public sealed class PopupSingleton : MonoBehaviour
    {
        private static PopupSingleton instance;
        public static PopupSingleton Instance => instance;

        private PopupUIView view;
        private Action confirmAction;
        private Action cancelAction;


        private void Awake()
        {
            TryGetComponent<PopupUIView>(out view);

            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            view.ConfirmButton.onClick.AddListener(OnConfirmClicked);
            view.CancelButton.onClick.AddListener(OnCancelClicked);
            view.HideImmediate();
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            view.ConfirmButton.onClick.RemoveListener(OnConfirmClicked);
            view.CancelButton.onClick.RemoveListener(OnCancelClicked);
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            instance = null;
        }

        public void ShowMessage(string message, Action onConfirm = null)
        {
            confirmAction = onConfirm;
            cancelAction = null;
            view.Show(message, false);
        }

        public void ShowConfirm(string message, Action onConfirm, Action onCancel = null)
        {
            confirmAction = onConfirm;
            cancelAction = onCancel;
            view.Show(message, true);
        }

        public void Hide()
        {
            confirmAction = null;
            cancelAction = null;
            _ = view.HideAsync();
        }

        private void OnConfirmClicked()
        {
            Action action = confirmAction;
            confirmAction = null;
            cancelAction = null;
            _ = HideAsync(action);
        }

        private void OnCancelClicked()
        {
            Action action = cancelAction;
            confirmAction = null;
            cancelAction = null;
            _ = HideAsync(action);
        }

        private async Awaitable HideAsync(Action onHidden)
        {
            if (await view.HideAsync())
                onHidden?.Invoke();
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            confirmAction = null;
            cancelAction = null;
            view.HideImmediate();
        }
    }
}
