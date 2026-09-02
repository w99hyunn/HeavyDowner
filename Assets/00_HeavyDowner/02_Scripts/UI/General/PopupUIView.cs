using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    public class PopupUIView : MonoBehaviour
    {
        [SerializeField] private GameObject background;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Animator popupAnimator;
        [SerializeField] private float hideDuration = 0.14f;

        private int animationVersion;

        public Button ConfirmButton => confirmButton;
        public Button CancelButton => cancelButton;

        public void Show(string message, bool showCancelButton)
        {
            animationVersion++;
            messageText.text = message;
            cancelButton.gameObject.SetActive(showCancelButton);
            confirmButton.interactable = true;
            cancelButton.interactable = true;

            RectTransform confirmButtonTransform = (RectTransform)confirmButton.transform;
            Vector2 confirmButtonPosition = confirmButtonTransform.anchoredPosition;
            confirmButtonPosition.x = showCancelButton ? 160f : 0f;
            confirmButtonTransform.anchoredPosition = confirmButtonPosition;

            background.SetActive(true);
            popupAnimator.Play("Show", 0, 0f);
        }

        public async Awaitable<bool> HideAsync()
        {
            if (!background.activeSelf)
                return false;

            int hideVersion = ++animationVersion;
            confirmButton.interactable = false;
            cancelButton.interactable = false;
            popupAnimator.Play("Hide", 0, 0f);

            try
            {
                await Awaitable.WaitForSecondsAsync(hideDuration, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            if (hideVersion != animationVersion)
                return false;

            background.SetActive(false);
            return true;
        }

        public void HideImmediate()
        {
            animationVersion++;
            background.SetActive(false);
        }
    }
}
