using System.Collections.Generic;
using Unity.Services.CloudSave;
using UnityEngine;

namespace HeavyDowner.Module
{
    public static class PlayerDataService
    {
        private const string NICKNAME_KEY = "Nickname";
        private const string CURRENCY_KEY = "Currency";
        private const string HIGH_SCORE_KEY = "HighScore";
        private const string DEFAULT_NICKNAME = "Downer";

        public static string Nickname { get; private set; } = DEFAULT_NICKNAME;
        public static int Currency { get; private set; }
        public static int HighScore { get; private set; }

        public static async Awaitable LoadAsync()
        {
            var keys = new HashSet<string>
            {
                NICKNAME_KEY,
                CURRENCY_KEY,
                HIGH_SCORE_KEY
            };
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            var defaults = new Dictionary<string, object>();

            if (data.TryGetValue(NICKNAME_KEY, out var nicknameItem))
            {
                Nickname = nicknameItem.Value.GetAs<string>();
            }
            else
            {
                Nickname = DEFAULT_NICKNAME;
                defaults[NICKNAME_KEY] = Nickname;
            }

            if (data.TryGetValue(CURRENCY_KEY, out var currencyItem))
            {
                Currency = currencyItem.Value.GetAs<int>();
            }
            else
            {
                Currency = 100;
                defaults[CURRENCY_KEY] = Currency;
            }

            if (data.TryGetValue(HIGH_SCORE_KEY, out var highScoreItem))
            {
                HighScore = highScoreItem.Value.GetAs<int>();
            }
            else
            {
                HighScore = 0;
                defaults[HIGH_SCORE_KEY] = HighScore;
            }

            if (defaults.Count > 0)
                await CloudSaveService.Instance.Data.Player.SaveAsync(defaults);
        }

        public static async Awaitable SaveNicknameAsync(string nickname)
        {
            string value = string.IsNullOrWhiteSpace(nickname) ? DEFAULT_NICKNAME : nickname.Trim();
            var data = new Dictionary<string, object>
            {
                [NICKNAME_KEY] = value
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Nickname = value;
        }

        public static async Awaitable SaveCurrencyAsync(int currency)
        {
            var data = new Dictionary<string, object>
            {
                [CURRENCY_KEY] = currency
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Currency = currency;
        }

        public static async Awaitable AddCurrencyAsync(int amount)
        {
            await SaveCurrencyAsync(Currency + amount);
        }

        public static async Awaitable SaveHighScoreAsync(int score)
        {
            if (score <= HighScore)
                return;

            var data = new Dictionary<string, object>
            {
                [HIGH_SCORE_KEY] = score
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            HighScore = score;
        }

        public static async Awaitable SaveAsync(string nickname, int currency)
        {
            string nicknameValue = string.IsNullOrWhiteSpace(nickname) ? DEFAULT_NICKNAME : nickname.Trim();
            var data = new Dictionary<string, object>
            {
                [NICKNAME_KEY] = nicknameValue,
                [CURRENCY_KEY] = currency
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Nickname = nicknameValue;
            Currency = currency;
        }
    }
}
