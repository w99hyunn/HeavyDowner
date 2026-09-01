using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.Gameplay
{
    public sealed class CellHealthBar : MonoBehaviour
    {
        private Slider healthSlider;
        private float width;

        private void Awake()
        {
            TryGetComponent<Slider>(out healthSlider);
        }

        public void Show(Vector3 position, float barWidth, float normalizedHealth)
        {
            transform.position = position;
            float rectWidth = barWidth / Mathf.Abs(transform.lossyScale.x);
            if (!Mathf.Approximately(width, rectWidth))
            {
                width = rectWidth;
                ((RectTransform)transform).SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    width);
            }

            healthSlider.SetValueWithoutNotify(normalizedHealth);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
