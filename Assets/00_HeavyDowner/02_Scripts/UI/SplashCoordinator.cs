using UnityEngine;
using HeavyDowner.Module;

namespace HeavyDowner.UI
{
    public class SplashCoordinator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup logoCanvasGroup;
        [SerializeField] private float fadeInDuration = 1f;
        [SerializeField] private float displayDuration = 1f;
        [SerializeField] private float fadeOutDuration = 1f;

        private void Awake()
        {
            logoCanvasGroup.alpha = 0f;
        }

        private async Awaitable Start()
        {
            await FadeTransition.FadeInAsync(logoCanvasGroup, fadeInDuration);
            await Awaitable.WaitForSecondsAsync(displayDuration);
            await FadeTransition.FadeOutAsync(logoCanvasGroup, fadeOutDuration);
            await LoadingBridgeService.LoadAsync(SceneType.Login, LoadingMode.None);
        }
    }
}
