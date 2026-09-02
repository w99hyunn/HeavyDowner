using HeavyDowner.Gameplay;
using HeavyDowner.Module;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.UI
{
    public class IngameUIController : MonoBehaviour
    {
        [SerializeField] private IngamePlayerController player;

        private IngameUIView view;
        private PlayerAbilitySystem abilitySystem;

        private void Awake()
        {
            TryGetComponent<IngameUIView>(out view);
            player.TryGetComponent<PlayerAbilitySystem>(out abilitySystem);
        }

        private void OnEnable()
        {
            player.HealthChanged += view.SetHealth;
            player.ShieldChanged += view.SetShield;
            player.DepthChanged += view.SetDepth;
            view.MenuButton.onClick.AddListener(HandleMenuClicked);
            for (int i = 0; i < view.SkillButtonCount; i++)
            {
                view.GetSkillButton(i).Clicked += HandleSkillClicked;
            }

            abilitySystem.AvailabilityChanged += RefreshSkillState;
            abilitySystem.CooldownStarted += HandleCooldownStarted;
        }

        private void Start()
        {
            view.SetHealth(player.HealthNormalized);
            view.SetShield(player.ShieldNormalized);
            view.SetDepth(player.CurrentDepth);
            for (int i = 0; i < view.SkillButtonCount; i++)
            {
                view.SetSkillIcon(i, abilitySystem.GetSkillDefinition(view.GetSkillButton(i).SlotId).Icon);
            }

            RefreshSkillState();
        }

        private void OnDisable()
        {
            player.HealthChanged -= view.SetHealth;
            player.ShieldChanged -= view.SetShield;
            player.DepthChanged -= view.SetDepth;
            view.MenuButton.onClick.RemoveListener(HandleMenuClicked);
            for (int i = 0; i < view.SkillButtonCount; i++)
            {
                view.GetSkillButton(i).Clicked -= HandleSkillClicked;
            }

            abilitySystem.AvailabilityChanged -= RefreshSkillState;
            abilitySystem.CooldownStarted -= HandleCooldownStarted;
        }

        private void HandleMenuClicked()
        {
            PopupSingleton.Instance.ShowConfirm("메인으로 이동하시겠습니까?", LoadMain);
        }

        private async void LoadMain()
        {
            view.MenuButton.interactable = false;
            await LoadingBridgeService.LoadAsync(SceneType.Main, LoadingMode.Overlay);
        }

        private void HandleSkillClicked(SkillSlotId slotId)
        {
            abilitySystem.TryActivate(slotId);
        }

        private void RefreshSkillState()
        {
            for (int i = 0; i < view.SkillButtonCount; i++)
            {
                view.SetSkillState(i, abilitySystem.CanActivate(view.GetSkillButton(i).SlotId));
            }
        }

        private void HandleCooldownStarted(SkillSlotId slotId)
        {
            for (int i = 0; i < view.SkillButtonCount; i++)
            {
                SkillButtonView skillButton = view.GetSkillButton(i);
                if (skillButton.SlotId == slotId)
                {
                    _ = UpdateSkillCooldownAsync(skillButton);
                    return;
                }
            }
        }

        private async Awaitable UpdateSkillCooldownAsync(SkillButtonView skillButton)
        {
            CancellationToken cancellationToken = destroyCancellationToken;
            while (!cancellationToken.IsCancellationRequested)
            {
                float remaining = abilitySystem.GetCooldownRemainingNormalized(skillButton.SlotId);
                skillButton.SetCooldown(remaining);
                if (remaining <= 0f)
                {
                    return;
                }

                await Awaitable.NextFrameAsync();
            }
        }
    }
}
