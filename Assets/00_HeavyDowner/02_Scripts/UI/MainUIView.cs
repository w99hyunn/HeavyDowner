using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeavyDowner.UI
{
    public sealed class MainUIView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private Button addCurrencyButton;
        [SerializeField] private Button gameStartButton;

        public Button AddCurrencyButton => addCurrencyButton;
        public Button GameStartButton => gameStartButton;

        public void SetPlayerData(string nickname, int currency, int highScore)
        {
            nicknameText.text = nickname;
            currencyText.text = currency.ToString();
            highScoreText.text = highScore.ToString();
        }
    }
}
