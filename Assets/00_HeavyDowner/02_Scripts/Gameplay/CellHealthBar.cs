using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.Gameplay
{
    public sealed class CellHealthBar : MonoBehaviour
    {
        [SerializeField] private RectTransform barRect;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private float pixelsPerUnit = 100f;

        private float width;

        public void Show(Vector3 position, float barWidth, float normalizedHealth)
        {
            transform.position = position;
            if (!Mathf.Approximately(width, barWidth))
            {
                width = barWidth;
                barRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    width * pixelsPerUnit);
            }

            SetValue(normalizedHealth);
            gameObject.SetActive(true);
        }

        public void SetValue(float normalizedHealth)
        {
            healthSlider.SetValueWithoutNotify(normalizedHealth);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
