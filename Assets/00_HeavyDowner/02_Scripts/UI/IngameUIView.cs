using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    public sealed class IngameUIView : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text positionText;

        private float targetHealth;

        private void Awake()
        {
            targetHealth = healthSlider.value;
        }

        private void Update()
        {
            float lerpAmount = 1f - Mathf.Exp(-8f * Time.deltaTime);
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealth, lerpAmount);
        }

        public void SetHealth(float normalizedHealth)
        {
            targetHealth = normalizedHealth;
        }

        public void SetDepth(int depth)
        {
            positionText.text = $"{depth}M";
        }
    }
}
