using System;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.UI
{
    public class PopupAnimation
    {
        private readonly GameObject root;
        private readonly Animator animator;
        private int animationVersion;

        public PopupAnimation(GameObject root, Animator animator)
        {
            this.root = root;
            this.animator = animator;
        }

        public bool IsVisible => root.activeSelf;

        public void Show()
        {
            animationVersion++;
            root.SetActive(true);
            animator.Play("Show", 0, 0f);
        }

        public async Awaitable<bool> HideAsync(CancellationToken cancellationToken)
        {
            int hideVersion = ++animationVersion;
            animator.Play("Hide", 0, 0f);

            try
            {
                await Awaitable.WaitForSecondsAsync(0.14f, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            if (hideVersion != animationVersion)
            {
                return false;
            }

            root.SetActive(false);
            return true;
        }

        public void HideImmediate()
        {
            animationVersion++;
            root.SetActive(false);
        }
    }
}
