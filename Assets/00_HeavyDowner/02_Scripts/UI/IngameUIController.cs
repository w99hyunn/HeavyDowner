using HeavyDowner.Gameplay;
using UnityEngine;

namespace HeavyDowner.UI
{
    public sealed class IngameUIController : MonoBehaviour
    {
        [SerializeField] private IngamePlayerController player;

        private IngameUIView view;

        private void Awake()
        {
            TryGetComponent<IngameUIView>(out view);
        }

        private void OnEnable()
        {
            player.HealthChanged += view.SetHealth;
            player.DepthChanged += view.SetDepth;
        }

        private void Start()
        {
            view.SetHealth(player.HealthNormalized);
            view.SetDepth(player.CurrentDepth);
        }

        private void OnDisable()
        {
            player.HealthChanged -= view.SetHealth;
            player.DepthChanged -= view.SetDepth;
        }
    }
}
