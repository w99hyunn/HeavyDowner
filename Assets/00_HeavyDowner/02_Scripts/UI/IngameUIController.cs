using System.Threading;
using HeavyDowner.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace HeavyDowner.UI
{
    public sealed class IngameUIController : MonoBehaviour
    {
        [SerializeField] private IngamePlayerController player;

        private IngameUIView view;
        private PlayerAbilitySystem abilitySystem;
        private UnityAction[] skillButtonActions;

        private void Awake()
        {
            TryGetComponent<IngameUIView>(out view);
            player.TryGetComponent<PlayerAbilitySystem>(out abilitySystem);
            skillButtonActions = new UnityAction[view.SkillButtonCount];
            for (int i = 0; i < skillButtonActions.Length; i++)
            {
                SkillSlotId slotId = view.GetSkillButton(i).SlotId;
                skillButtonActions[i] = () => abilitySystem.TryActivate(slotId);
            }
        }

        private void OnEnable()
        {
            player.HealthChanged += view.SetHealth;
            player.ShieldChanged += view.SetShield;
            player.DepthChanged += view.SetDepth;
            for (int i = 0; i < skillButtonActions.Length; i++)
            {
                view.GetSkillButton(i).Button.onClick.AddListener(skillButtonActions[i]);
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
                SkillSlotId slotId = view.GetSkillButton(i).SlotId;
                view.SetSkillIcon(i, abilitySystem.GetSkillDefinition(slotId).Icon);
            }

            RefreshSkillState();
        }

        private void OnDisable()
        {
            player.HealthChanged -= view.SetHealth;
            player.ShieldChanged -= view.SetShield;
            player.DepthChanged -= view.SetDepth;
            for (int i = 0; i < skillButtonActions.Length; i++)
            {
                view.GetSkillButton(i).Button.onClick.RemoveListener(skillButtonActions[i]);
            }

            abilitySystem.AvailabilityChanged -= RefreshSkillState;
            abilitySystem.CooldownStarted -= HandleCooldownStarted;
        }

        private void RefreshSkillState()
        {
            for (int i = 0; i < view.SkillButtonCount; i++)
            {
                SkillSlotId slotId = view.GetSkillButton(i).SlotId;
                view.SetSkillState(i, abilitySystem.CanActivate(slotId));
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
