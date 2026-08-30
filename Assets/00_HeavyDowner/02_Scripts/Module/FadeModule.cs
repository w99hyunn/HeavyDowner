using UnityEngine;

namespace HeavyDowner.Module
{
    public static class FadeModule
    {
        public static Awaitable FadeInAsync(CanvasGroup canvasGroup, float duration)
        {
            return FadeAsync(canvasGroup, 0f, 1f, duration);
        }

        public static Awaitable FadeOutAsync(CanvasGroup canvasGroup, float duration)
        {
            return FadeAsync(canvasGroup, canvasGroup.alpha, 0f, duration);
        }

        private static async Awaitable FadeAsync(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
        {
            canvasGroup.alpha = startAlpha;

            if (duration <= 0f)
            {
                canvasGroup.alpha = endAlpha;
                return;
            }

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                await Awaitable.NextFrameAsync();
            }

            canvasGroup.alpha = endAlpha;
        }
    }
}