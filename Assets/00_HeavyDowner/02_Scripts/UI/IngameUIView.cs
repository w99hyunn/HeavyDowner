using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    public sealed class IngameUIView : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TMP_Text positionText;

        private float targetHealth;
        private Color healthFillColor;

        private void Awake()
        {
            targetHealth = healthSlider.value;
            healthFillColor = healthFillImage.color;
        }

        private void Update()
        {
            float lerpAmount = 1f - Mathf.Exp(-8f * Time.deltaTime);
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealth, lerpAmount);
        }

        public void SetHealth(float normalizedHealth)
        {
            bool healthDecreased = normalizedHealth < targetHealth;
            targetHealth = normalizedHealth;

            if (healthDecreased)
                _ = FlashHealthFillAsync();
        }

        public void SetDepth(int depth)
        {
            positionText.text = $"{depth}M";
        }

        private async Awaitable FlashHealthFillAsync()
        {
            Color fadedColor = healthFillColor;
            fadedColor.a = 0.25f;

            try
            {
                for (int i = 0; i < 2; i++)
                {
                    healthFillImage.color = fadedColor;
                    await Awaitable.WaitForSecondsAsync(0.07f, destroyCancellationToken);
                    healthFillImage.color = healthFillColor;
                    await Awaitable.WaitForSecondsAsync(0.07f, destroyCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
