using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    public class IngameUIView : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private RectTransform shieldFillRect;
        [SerializeField] private TMP_Text positionText;
        [SerializeField] private Button menuButton;
        [SerializeField] private SkillButtonView[] skillButtons;

        private Image healthFillImage;
        private float targetHealth;
        private float targetShield;
        private float displayedShield;
        private Color healthFillColor;

        public Button MenuButton => menuButton;
        public int SkillButtonCount => skillButtons.Length;

        public SkillButtonView GetSkillButton(int slotIndex)
        {
            return skillButtons[slotIndex];
        }

        private void Awake()
        {
            healthSlider.fillRect.TryGetComponent<Image>(out healthFillImage);
            targetHealth = healthSlider.value;
            targetShield = 0f;
            displayedShield = 0f;
            healthFillColor = healthFillImage.color;

            Vector2 shieldAnchorMin = shieldFillRect.anchorMin;
            Vector2 shieldAnchorMax = shieldFillRect.anchorMax;
            shieldAnchorMin.x = targetHealth;
            shieldAnchorMax.x = targetHealth;
            shieldFillRect.anchorMin = shieldAnchorMin;
            shieldFillRect.anchorMax = shieldAnchorMax;
        }

        private void Update()
        {
            float lerpAmount = 1f - Mathf.Exp(-8f * Time.deltaTime);
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealth, lerpAmount);
            displayedShield = Mathf.Lerp(displayedShield, targetShield, lerpAmount);

            float shieldEnd = Mathf.Min(1f, healthSlider.value + displayedShield);
            Vector2 shieldAnchorMin = shieldFillRect.anchorMin;
            Vector2 shieldAnchorMax = shieldFillRect.anchorMax;
            shieldAnchorMin.x = shieldEnd - displayedShield;
            shieldAnchorMax.x = shieldEnd;
            shieldFillRect.anchorMin = shieldAnchorMin;
            shieldFillRect.anchorMax = shieldAnchorMax;
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

        public void SetShield(float normalizedShield)
        {
            targetShield = normalizedShield;
        }

        public void SetSkillIcon(int slotIndex, Sprite icon)
        {
            skillButtons[slotIndex].SetIcon(icon);
        }

        public void SetSkillState(int slotIndex, bool ready)
        {
            skillButtons[slotIndex].SetInteractable(ready);
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
