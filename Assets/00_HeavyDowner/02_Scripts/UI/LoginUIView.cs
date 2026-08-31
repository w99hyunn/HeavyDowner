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

        public void ShowPreparing()
        {
            loadingText.text = "로그인 준비 중..";
            googleLoginButton.gameObject.SetActive(false);
            guestLoginButton.gameObject.SetActive(false);
        }

        public void ShowLogin()
        {
            loadingText.text = "";
            googleLoginButton.gameObject.SetActive(true);
            googleLoginButton.interactable = true;
            guestLoginButton.gameObject.SetActive(true);
            guestLoginButton.interactable = true;
        }

        public void ShowSigningIn()
        {
            loadingText.text = "로그인 중..";
            googleLoginButton.interactable = false;
            guestLoginButton.interactable = false;
        }

        public void ShowGuestSigningIn()
        {
            loadingText.text = "로그인 중..";
            googleLoginButton.interactable = false;
            guestLoginButton.interactable = false;
        }

        public void ShowLoginFailed()
        {
            loadingText.text = "";
            googleLoginButton.gameObject.SetActive(true);
            googleLoginButton.interactable = true;
            guestLoginButton.gameObject.SetActive(true);
            guestLoginButton.interactable = true;
            PopupSingleton.Instance.ShowMessage("로그인에 실패했습니다.\r\n다시 시도해주세요.");
        }

        public void ShowCompleted()
        {
            loadingText.text = "로그인 완료";
            googleLoginButton.gameObject.SetActive(false);
            guestLoginButton.gameObject.SetActive(false);
        }
    }
}
