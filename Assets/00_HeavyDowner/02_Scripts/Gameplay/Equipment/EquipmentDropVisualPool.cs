using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public sealed class EquipmentDropVisualPool : MonoBehaviour
    {
        [SerializeField] private Transform[] roots;
        [SerializeField] private SpriteRenderer[] icons;
        [SerializeField] private ParticleSystem[] effects;
        [SerializeField, Min(0.01f)] private float riseDuration = 0.35f;
        [SerializeField, Min(0f)] private float riseHeight = 0.7f;
        [SerializeField, Min(0f)] private float hoverDuration = 1.1f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.45f;
        [SerializeField, Min(0f)] private float hoverAmplitude = 0.08f;
        [SerializeField, Min(0f)] private float hoverSpeed = 6f;

        private bool[] playing;
        private float[] startedAt;
        private Vector3[] origins;
        private int nextVisual;

        private void Awake()
        {
            playing = new bool[roots.Length];
            startedAt = new float[roots.Length];
            origins = new Vector3[roots.Length];
            for (int index = 0; index < roots.Length; index++)
            {
                roots[index].gameObject.SetActive(false);
            }

            enabled = false;
        }

        private void Update()
        {
            bool hasPlayingVisual = false;
            float fadeStart = riseDuration + hoverDuration;
            float totalDuration = fadeStart + fadeDuration;

            for (int index = 0; index < roots.Length; index++)
            {
                if (!playing[index])
                {
                    continue;
                }

                float elapsed = Time.time - startedAt[index];
                if (elapsed >= totalDuration)
                {
                    Stop(index);
                    continue;
                }

                float riseProgress = Mathf.Clamp01(elapsed / riseDuration);
                float easedRise = 1f - Mathf.Pow(1f - riseProgress, 3f);
                float hover = elapsed > riseDuration
                    ? Mathf.Sin((elapsed - riseDuration) * hoverSpeed) * hoverAmplitude
                    : 0f;
                roots[index].position = origins[index] + Vector3.up * (riseHeight * easedRise + hover);

                float alpha = elapsed > fadeStart
                    ? 1f - (elapsed - fadeStart) / fadeDuration
                    : 1f;
                Color color = icons[index].color;
                color.a = alpha;
                icons[index].color = color;
                hasPlayingVisual = true;
            }

            enabled = hasPlayingVisual;
        }

        public void Play(Sprite icon, Vector3 worldPosition)
        {
            int index = GetAvailableIndex();
            playing[index] = true;
            startedAt[index] = Time.time;
            origins[index] = worldPosition;
            roots[index].position = worldPosition;
            icons[index].sprite = icon;
            icons[index].color = Color.white;
            roots[index].gameObject.SetActive(true);
            effects[index].Clear(true);
            effects[index].Play(true);
            enabled = true;
        }

        private int GetAvailableIndex()
        {
            for (int offset = 0; offset < roots.Length; offset++)
            {
                int index = (nextVisual + offset) % roots.Length;
                if (!playing[index])
                {
                    nextVisual = (index + 1) % roots.Length;
                    return index;
                }
            }

            int fallback = nextVisual;
            nextVisual = (nextVisual + 1) % roots.Length;
            Stop(fallback);
            return fallback;
        }

        private void Stop(int index)
        {
            playing[index] = false;
            effects[index].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            roots[index].gameObject.SetActive(false);
        }
    }
}
