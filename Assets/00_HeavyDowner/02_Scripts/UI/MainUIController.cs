using HeavyDowner.Module;
using Unity.Services.Core;
using UnityEngine;

namespace HeavyDowner.UI
{
    public class MainUIController : MonoBehaviour
    {
        private const int CURRENCY_RECOVERY_AMOUNT = 100;
        private const int GAME_START_COST = 5;

        private MainUIView view;
        private EquipmentUIController equipmentUI;
        private bool isStartingGame;

        private void Awake()
        {
            TryGetComponent<MainUIView>(out view);
            TryGetComponent<EquipmentUIController>(out equipmentUI);
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            view.AddCurrencyButton.onClick.AddListener(OnAddCurrencyClicked);
            view.EquipmentButton.onClick.AddListener(equipmentUI.Show);
            view.GameStartButton.onClick.AddListener(OnGameStartClicked);
            equipmentUI.EnhancementOrbsChanged += Refresh;
        }

        private void OnDisable()
        {
            view.AddCurrencyButton.onClick.RemoveListener(OnAddCurrencyClicked);
            view.EquipmentButton.onClick.RemoveListener(equipmentUI.Show);
            view.GameStartButton.onClick.RemoveListener(OnGameStartClicked);
            equipmentUI.EnhancementOrbsChanged -= Refresh;
        }

        private void Refresh()
        {
            view.SetPlayerData(PlayerDataService.Nickname, PlayerDataService.Currency, PlayerDataService.EnhancementOrbs, PlayerDataService.HighScore);
        }

        private void OnAddCurrencyClicked()
        {
            PopupSingleton.Instance.ShowConfirm($"기력 {CURRENCY_RECOVERY_AMOUNT}을 회복하시겠습니까?", OnConfirmAddCurrency);
        }

        private async void OnConfirmAddCurrency()
        {
            await AddCurrencyAsync();
        }

        private async Awaitable AddCurrencyAsync()
        {
            try
            {
                await PlayerDataService.AddCurrencyAsync(CURRENCY_RECOVERY_AMOUNT);
            }
            catch (RequestFailedException exception)
            {
                Debug.LogException(exception);
                PopupSingleton.Instance.ShowMessage("기력 회복에 실패했습니다.\r\n다시 시도해주세요.");
                return;
            }

            Refresh();
        }

        private async void OnGameStartClicked()
        {
            if (isStartingGame)
                return;

            if (PlayerDataService.Currency < GAME_START_COST)
            {
                PopupSingleton.Instance.ShowMessage("기력이 부족합니다.");
                return;
            }

            await StartGameAsync();
        }

        private async Awaitable StartGameAsync()
        {
            isStartingGame = true;

            try
            {
                await PlayerDataService.SaveCurrencyAsync(PlayerDataService.Currency - GAME_START_COST);
            }
            catch (RequestFailedException exception)
            {
                isStartingGame = false;
                Debug.LogException(exception);
                PopupSingleton.Instance.ShowMessage("게임 시작에 실패했습니다.\r\n다시 시도해주세요.");
                return;
            }

            Refresh();
            await LoadingBridgeService.LoadAsync(SceneType.Ingame, LoadingMode.Overlay);
        }
    }
}
