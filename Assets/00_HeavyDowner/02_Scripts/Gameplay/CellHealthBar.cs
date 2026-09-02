using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.Gameplay
{
    public class CellHealthBar : MonoBehaviour
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
                ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            gameObject.SetActive(true);
            healthSlider.SetValueWithoutNotify(normalizedHealth);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
