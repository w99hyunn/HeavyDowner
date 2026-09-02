using UnityEngine;
using UnityEngine.Rendering;

namespace HeavyDowner.Gameplay
{
    public class BossHitPresentation : MonoBehaviour
    {
        [SerializeField] private Volume hitVolume;
        [SerializeField, Min(0f)] private float freezeDuration = 1.5f;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.2f;

        private IngamePlayerController player;
        private int playbackVersion;
        private bool isPlaying;

        private void Awake()
        {
            TryGetComponent<IngamePlayerController>(out player);
        }

        private void OnEnable()
        {
            isPlaying = false;
            hitVolume.weight = 0f;
        }

        private void OnDisable()
        {
            playbackVersion++;
            player.SetHitFrozen(false);
            isPlaying = false;
        }

        public void Play()
        {
            if (isPlaying)
            {
                return;
            }

            isPlaying = true;
            player.SetHitFrozen(true);
            hitVolume.weight = 1f;
            _ = PlayAsync(++playbackVersion);
        }

        private async Awaitable PlayAsync(int version)
        {
            await WaitUnscaledAsync(freezeDuration, version);
            if (version != playbackVersion)
            {
                return;
            }

            player.SetHitFrozen(false);
            await TransitionVolumeAsync(0f, recoveryDuration, version);
            if (version != playbackVersion)
            {
                return;
            }

            isPlaying = false;
        }

        private async Awaitable WaitUnscaledAsync(float duration, int version)
        {
            float elapsed = 0f;
            while (elapsed < duration && version == playbackVersion)
            {
                elapsed += Time.unscaledDeltaTime;
                await Awaitable.NextFrameAsync();
            }
        }

        private async Awaitable TransitionVolumeAsync(float targetWeight, float duration, int version)
        {
            float elapsed = 0f;
            float startWeight = hitVolume.weight;

            while (elapsed < duration && version == playbackVersion)
            {
                elapsed += Time.unscaledDeltaTime;
                hitVolume.weight = Mathf.Lerp(startWeight, targetWeight, Mathf.Clamp01(elapsed / duration));
                await Awaitable.NextFrameAsync();
            }

            if (version == playbackVersion)
            {
                hitVolume.weight = targetWeight;
            }
        }
    }
}
