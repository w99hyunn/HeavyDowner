using HeavyDowner.Gameplay;
using HeavyDowner.Module;
using Unity.Services.Core;
using UnityEngine;

namespace HeavyDowner.UI
{
    public sealed class GameResultUIController : MonoBehaviour
    {
        [SerializeField] private IngamePlayerController player;
        [SerializeField] private VerticalCameraFollower cameraFollower;
        [SerializeField] private RunRewardSession rewardSession;

        private GameResultUIView view;
        private bool isSaving;
        private bool isShown;

        private void Awake()
        {
            TryGetComponent<GameResultUIView>(out view);
        }

        private void OnEnable()
        {
            cameraFollower.ReachedTop += OnCameraReachedTop;
            view.MainButton.onClick.AddListener(OnMainClicked);
        }

        private void OnDisable()
        {
            cameraFollower.ReachedTop -= OnCameraReachedTop;
            view.MainButton.onClick.RemoveListener(OnMainClicked);
        }

        private async void OnCameraReachedTop()
        {
            if (isShown)
            {
                return;
            }

            isShown = true;
            view.Show(player.CurrentDepth, rewardSession.EnhancementOrbs, rewardSession.AcquiredEquipment);
            await SaveRunAsync();
        }

        private async void OnMainClicked()
        {
            if (isSaving)
            {
                return;
            }

            if (!rewardSession.IsCommitted)
            {
                await SaveRunAsync();
                return;
            }

            if (await view.HideAsync())
                await LoadingBridgeService.LoadAsync(SceneType.Main, LoadingMode.Overlay);
        }

        private async Awaitable SaveRunAsync()
        {
            isSaving = true;
            view.SetSaving();
            try
            {
                await rewardSession.CommitAsync(player.CurrentDepth);
            }
            catch (RequestFailedException exception)
            {
                Debug.LogException(exception);
                view.SetSaveFailed();
                isSaving = false;
                return;
            }

            isSaving = false;
            view.SetSaved();
        }
    }
}
