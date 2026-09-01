using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

namespace HeavyDowner.UI
{
    public class JoystickControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private CanvasGroup joystickBackground;
        [SerializeField] private OnScreenStick onScreenStick;

        public Vector2 Direction { get; private set; }

        private void Awake()
        {
            joystickBackground.alpha = 0f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            joystickBackground.transform.position = eventData.position;
            joystickBackground.alpha = 1f;
            ExecuteEvents.Execute(onScreenStick.gameObject, eventData, ExecuteEvents.pointerDownHandler);
            UpdateDirection(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ExecuteEvents.Execute(onScreenStick.gameObject, eventData, ExecuteEvents.dragHandler);
            UpdateDirection(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ExecuteEvents.Execute(onScreenStick.gameObject, eventData, ExecuteEvents.pointerUpHandler);
            Direction = Vector2.zero;
            joystickBackground.alpha = 0f;
        }

        private void UpdateDirection(PointerEventData eventData)
        {
            RectTransform backgroundRect = (RectTransform)joystickBackground.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                backgroundRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            Direction = Vector2.ClampMagnitude(localPoint / onScreenStick.movementRange, 1f);
        }
    }
}
