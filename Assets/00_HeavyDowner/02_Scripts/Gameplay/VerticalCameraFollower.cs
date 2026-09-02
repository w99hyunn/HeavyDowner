using System;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class VerticalCameraFollower : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float verticalOffset = 1.5f;
        [SerializeField] private float followSpeed = 8f;
        [SerializeField] private float scrollToTopSpeed = 3f;

        private IngamePlayerController player;
        private float initialY;
        private bool isScrollingToTop;

        public event Action ReachedTop;

        private void Awake()
        {
            initialY = transform.position.y;
            target.TryGetComponent<IngamePlayerController>(out player);
        }

        private void OnEnable()
        {
            player.Died += ScrollToTop;
        }

        private void OnDisable()
        {
            player.Died -= ScrollToTop;
        }

        private void LateUpdate()
        {
            if (isScrollingToTop)
            {
                MoveToTop();
                return;
            }

            if (player.IsDead)
            {
                return;
            }

            float targetY = target.position.y + verticalOffset;
            if (targetY >= transform.position.y)
            {
                return;
            }

            Vector3 position = transform.position;
            float interpolation = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            position.y = Mathf.Lerp(position.y, targetY, interpolation);
            transform.position = position;
        }

        private void ScrollToTop()
        {
            isScrollingToTop = true;
        }

        private void MoveToTop()
        {
            Vector3 position = transform.position;
            position.y = Mathf.MoveTowards(position.y, initialY, scrollToTopSpeed * Time.deltaTime);
            transform.position = position;
            if (position.y == initialY)
            {
                isScrollingToTop = false;
                ReachedTop?.Invoke();
            }
        }
    }
}
