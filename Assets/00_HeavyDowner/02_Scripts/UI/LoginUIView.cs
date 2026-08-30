using TMPro;
using UnityEngine;

namespace HeavyDowner.UI
{
    public sealed class LoginUIView : MonoBehaviour
    {
        [SerializeField] private TMP_Text loadingText;

        public void ShowLoading()
        {
            loadingText.text = "데이터 불러오는 중...";
        }

        public void ShowCompleted()
        {
            loadingText.text = "로그인 완료";
        }
    }
}
