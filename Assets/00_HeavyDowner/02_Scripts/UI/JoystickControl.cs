using UnityEngine;
using UnityEngine.EventSystems;

namespace HeavyDowner.UI
{
    public class JoystickControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private CanvasGroup joystickBackground;
        [SerializeField] private GameObject stickHandler;

        private void Awake()
        {
            joystickBackground.alpha = 0f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            joystickBackground.transform.position = eventData.position;
            joystickBackground.alpha = 1f;
            ExecuteEvents.Execute(stickHandler, eventData, ExecuteEvents.pointerDownHandler);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ExecuteEvents.Execute(stickHandler, eventData, ExecuteEvents.dragHandler);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ExecuteEvents.Execute(stickHandler, eventData, ExecuteEvents.pointerUpHandler);
            joystickBackground.alpha = 0f;
        }
    }
}
