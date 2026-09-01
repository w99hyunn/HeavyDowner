using System;
using HeavyDowner.Module;
using UnityEngine;

namespace HeavyDowner.UI
{
    public sealed class MainUIController : MonoBehaviour
    {
        private const int CURRENCY_RECOVERY_AMOUNT = 100;
        private const int GAME_START_COST = 5;

        private MainUIView view;
        private bool isStartingGame;

        private void Awake()
        {
            TryGetComponent<MainUIView>(out view);
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            view.AddCurrencyButton.onClick.AddListener(OnAddCurrencyClicked);
            view.GameStartButton.onClick.AddListener(OnGameStartClicked);
        }

        private void OnDisable()
        {
            view.AddCurrencyButton.onClick.RemoveListener(OnAddCurrencyClicked);
            view.GameStartButton.onClick.RemoveListener(OnGameStartClicked);
        }

        private void Refresh()
        {
            view.SetPlayerData(
                PlayerDataService.Nickname,
                PlayerDataService.Currency,
                PlayerDataService.HighScore);
        }

        private void OnAddCurrencyClicked()
        {
            PopupSingleton.Instance.ShowConfirm(
                $"기력 {CURRENCY_RECOVERY_AMOUNT}을 회복하시겠습니까?",
                OnConfirmAddCurrency);
        }

        private void OnConfirmAddCurrency()
        {
            _ = AddCurrencyAsync();
        }

        private async Awaitable AddCurrencyAsync()
        {
            try
            {
                await PlayerDataService.AddCurrencyAsync(CURRENCY_RECOVERY_AMOUNT);
                Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                PopupSingleton.Instance.ShowMessage("기력 회복에 실패했습니다.\r\n다시 시도해주세요.");
            }
        }

        private void OnGameStartClicked()
        {
            if (isStartingGame)
                return;

            if (PlayerDataService.Currency < GAME_START_COST)
            {
                PopupSingleton.Instance.ShowMessage("기력이 부족합니다.");
                return;
            }

            _ = StartGameAsync();
        }

        private async Awaitable StartGameAsync()
        {
            isStartingGame = true;

            try
            {
                await PlayerDataService.SaveCurrencyAsync(PlayerDataService.Currency - GAME_START_COST);
                Refresh();
                await LoadingBridgeService.LoadAsync(SceneType.Ingame, LoadingMode.Overlay);
            }
            catch (Exception exception)
            {
                isStartingGame = false;
                Debug.LogException(exception);
                PopupSingleton.Instance.ShowMessage("게임 시작에 실패했습니다.\r\n다시 시도해주세요.");
            }
        }
    }
}
