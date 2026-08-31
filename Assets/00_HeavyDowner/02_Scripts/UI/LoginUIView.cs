using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace HeavyDowner.UI
{
    public sealed class LoginUIView : MonoBehaviour
    {
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private Button googleLoginButton;
        [SerializeField] private Button guestLoginButton;

        public Button GoogleLoginButton => googleLoginButton;
        public Button GuestLoginButton => guestLoginButton;

        public void SetMessage(string message)
        {
            loadingText.text = message;
        }

        public void ShowPreparing()
        {
            googleLoginButton.gameObject.SetActive(false);
            guestLoginButton.gameObject.SetActive(false);
        }

        public void ShowLogin()
        {
            googleLoginButton.gameObject.SetActive(true);
            googleLoginButton.interactable = true;
            guestLoginButton.gameObject.SetActive(true);
            guestLoginButton.interactable = true;
        }

        public void ShowSigningIn()
        {
            googleLoginButton.interactable = false;
            guestLoginButton.interactable = false;
        }

        public void ShowLoginFailed()
        {
            googleLoginButton.gameObject.SetActive(true);
            googleLoginButton.interactable = true;
            guestLoginButton.gameObject.SetActive(true);
            guestLoginButton.interactable = true;
            PopupSingleton.Instance.ShowMessage("로그인에 실패했습니다.\r\n다시 시도해주세요.");
        }

        public void ShowCompleted()
        {
            googleLoginButton.gameObject.SetActive(false);
            guestLoginButton.gameObject.SetActive(false);
        }
    }
}
