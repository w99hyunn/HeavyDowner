using System;
using System.Threading;
using TMPro;
using UnityEngine;

namespace HeavyDowner.UI
{
    public class LoadingVisual : MonoBehaviour
    {
        private TMP_Text loadingText;

        private void Awake()
        {
            TryGetComponent<TMP_Text>(out loadingText);
        }

        private async Awaitable Start()
        {
            CancellationToken cancellationToken = destroyCancellationToken;
            bool showThreeDots = false;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    loadingText.text = showThreeDots ? "로딩 중..." : "로딩 중..";
                    showThreeDots = !showThreeDots;
                    await Awaitable.WaitForSecondsAsync(0.5f, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
