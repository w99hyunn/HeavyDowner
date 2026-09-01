using System;
using System.Threading;
using TMPro;
using UnityEngine;

namespace HeavyDowner.UI
{
    public class LoadingVisual : MonoBehaviour
    {
        private async Awaitable Start()
        {
            TryGetComponent<TMP_Text>(out TMP_Text loadingText);
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
