using System;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public sealed class EquipmentDropVisualPool : MonoBehaviour
    {
        [Serializable]
        private sealed class VisualSlot
        {
            [SerializeField] private Transform root;
            [SerializeField] private SpriteRenderer icon;
            [SerializeField] private ParticleSystem effect;

            private float startedAt;
            private Vector3 origin;

            public bool IsPlaying { get; private set; }

            public void Initialize()
            {
                IsPlaying = false;
                root.gameObject.SetActive(false);
            }

            public void Play(Sprite sprite, Vector3 worldPosition)
            {
                startedAt = Time.time;
                origin = worldPosition;
                root.position = worldPosition;
                icon.sprite = sprite;
                icon.color = Color.white;
                root.gameObject.SetActive(true);
                effect.Clear(true);
                effect.Play(true);
                IsPlaying = true;
            }

            public bool Tick(float now, float riseDuration, float riseHeight, float hoverDuration, float hoverAmplitude, float hoverSpeed, float fadeDuration)
            {
                if (!IsPlaying)
                {
                    return false;
                }

                float elapsed = now - startedAt;
                float fadeStart = riseDuration + hoverDuration;
                if (elapsed >= fadeStart + fadeDuration)
                {
                    Stop();
                    return false;
                }

                float riseProgress = Mathf.Clamp01(elapsed / riseDuration);
                float easedRise = 1f - Mathf.Pow(1f - riseProgress, 3f);
                float hover = elapsed > riseDuration
                    ? Mathf.Sin((elapsed - riseDuration) * hoverSpeed) * hoverAmplitude
                    : 0f;
                root.position = origin + Vector3.up * (riseHeight * easedRise + hover);

                float alpha = elapsed > fadeStart
                    ? 1f - (elapsed - fadeStart) / fadeDuration
                    : 1f;
                Color color = icon.color;
                color.a = alpha;
                icon.color = color;
                return true;
            }

            public void Stop()
            {
                IsPlaying = false;
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                root.gameObject.SetActive(false);
            }
        }

        [SerializeField] private VisualSlot[] slots;
        [SerializeField, Min(0.01f)] private float riseDuration = 0.35f;
        [SerializeField, Min(0f)] private float riseHeight = 0.7f;
        [SerializeField, Min(0f)] private float hoverDuration = 1.1f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.45f;
        [SerializeField, Min(0f)] private float hoverAmplitude = 0.08f;
        [SerializeField, Min(0f)] private float hoverSpeed = 6f;

        private int nextVisual;

        private void Awake()
        {
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index].Initialize();
            }

            enabled = false;
        }

        private void Update()
        {
            bool hasPlayingVisual = false;
            for (int index = 0; index < slots.Length; index++)
            {
                hasPlayingVisual |= slots[index].Tick(Time.time, riseDuration, riseHeight, hoverDuration, hoverAmplitude, hoverSpeed, fadeDuration);
            }

            enabled = hasPlayingVisual;
        }

        public void Play(Sprite icon, Vector3 worldPosition)
        {
            GetAvailableSlot().Play(icon, worldPosition);
            enabled = true;
        }

        private VisualSlot GetAvailableSlot()
        {
            for (int offset = 0; offset < slots.Length; offset++)
            {
                int index = (nextVisual + offset) % slots.Length;
                if (!slots[index].IsPlaying)
                {
                    nextVisual = (index + 1) % slots.Length;
                    return slots[index];
                }
            }

            VisualSlot fallback = slots[nextVisual];
            nextVisual = (nextVisual + 1) % slots.Length;
            fallback.Stop();
            return fallback;
        }
    }
}
