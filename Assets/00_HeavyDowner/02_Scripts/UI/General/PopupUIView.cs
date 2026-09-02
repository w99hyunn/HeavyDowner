using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    public class PopupUIView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Animator popupAnimator;

        private PopupAnimation popupAnimation;

        public Button ConfirmButton => confirmButton;
        public Button CancelButton => cancelButton;

        private void Awake()
        {
            popupAnimation = new PopupAnimation(popupRoot, popupAnimator);
            popupAnimation.HideImmediate();
        }

        public void Show(string message, bool showCancelButton)
        {
            messageText.text = message;
            cancelButton.gameObject.SetActive(showCancelButton);
            confirmButton.interactable = true;
            cancelButton.interactable = true;

            RectTransform confirmButtonTransform = (RectTransform)confirmButton.transform;
            Vector2 confirmButtonPosition = confirmButtonTransform.anchoredPosition;
            confirmButtonPosition.x = showCancelButton ? 160f : 0f;
            confirmButtonTransform.anchoredPosition = confirmButtonPosition;

            popupAnimation.Show();
        }

        public async Awaitable<bool> HideAsync()
        {
            if (!popupAnimation.IsVisible)
            {
                return false;
            }

            confirmButton.interactable = false;
            cancelButton.interactable = false;
            return await popupAnimation.HideAsync(destroyCancellationToken);
        }

        public void HideImmediate()
        {
            popupAnimation.HideImmediate();
        }
    }
}
