using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class PlayerAbilitySystem : MonoBehaviour
    {
        [SerializeField] private SkillSlotDefinition[] slots;
        [SerializeField] private DescentBoardController board;

        private readonly Dictionary<SkillSlotId, GameplayAbilityRuntime> skillsBySlot = new();
        private readonly Dictionary<SkillDefinition, GameplayAbilityRuntime> pickupAbilities = new();
        private GameplayAbilitySystem abilitySystem;
        private IngamePlayerController player;
        private GameplayCuePlayer cuePlayer;
        private SkillExecutionContext executionContext;

        public event Action AvailabilityChanged;
        public event Action<SkillSlotId> CooldownStarted;

        private void Awake()
        {
            TryGetComponent<IngamePlayerController>(out player);
            TryGetComponent<GameplayCuePlayer>(out cuePlayer);

            executionContext = new SkillExecutionContext(player, board, cuePlayer);
            abilitySystem = new GameplayAbilitySystem(cuePlayer);
            foreach (SkillSlotDefinition slot in slots)
            {
                GameplayAbilityRuntime runtime = abilitySystem.GrantAbility(
                    new GameplayAbilityRuntime(slot.Skill, executionContext, destroyCancellationToken));
                runtime.StateChanged += HandleSkillStateChanged;
                skillsBySlot.Add(slot.SlotId, runtime);
            }
        }

        private void OnDisable()
        {
            abilitySystem.CancelAll();
        }

        public SkillDefinition GetSkillDefinition(SkillSlotId slotId)
        {
            return (SkillDefinition)skillsBySlot[slotId].Definition;
        }

        public float GetCooldownRemainingNormalized(SkillSlotId slotId)
        {
            GameplayAbilityRuntime runtime = skillsBySlot[slotId];
            if (runtime.Definition.Cooldown <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(runtime.CooldownRemaining / runtime.Definition.Cooldown);
        }

        public bool CanActivate(SkillSlotId slotId)
        {
            GameplayAbilityRuntime runtime = skillsBySlot[slotId];
            if (player.IsDead || !runtime.IsReady)
            {
                return false;
            }

            return (((SkillDefinition)runtime.Definition).RequiredFreeCapabilities & GetOccupiedCapabilities()) == SkillCapability.None;
        }

        public void TryActivate(SkillSlotId slotId)
        {
            if (!CanActivate(slotId))
            {
                return;
            }

            GameplayAbilityRuntime runtime = skillsBySlot[slotId];
            runtime.Activate();
            if (runtime.Definition.Cooldown > 0f)
            {
                CooldownStarted?.Invoke(slotId);
            }
        }

        public void TryActivatePickup(SkillDefinition ability)
        {
            if (!pickupAbilities.TryGetValue(ability, out GameplayAbilityRuntime runtime))
            {
                runtime = abilitySystem.GrantAbility(
                    new GameplayAbilityRuntime(ability, executionContext, destroyCancellationToken));
                runtime.StateChanged += HandleSkillStateChanged;
                pickupAbilities.Add(ability, runtime);
            }

            if (player.IsDead
                || !runtime.IsReady
                || (ability.RequiredFreeCapabilities & GetOccupiedCapabilities()) != SkillCapability.None)
            {
                return;
            }

            runtime.Activate();
        }

        private SkillCapability GetOccupiedCapabilities()
        {
            SkillCapability occupiedCapabilities = SkillCapability.None;
            foreach (GameplayAbilityRuntime runtime in skillsBySlot.Values)
            {
                if (runtime.IsActive)
                {
                    occupiedCapabilities |= ((SkillDefinition)runtime.Definition).OccupiedCapabilities;
                }
            }

            foreach (GameplayAbilityRuntime runtime in pickupAbilities.Values)
            {
                if (runtime.IsActive)
                {
                    occupiedCapabilities |= ((SkillDefinition)runtime.Definition).OccupiedCapabilities;
                }
            }

            return occupiedCapabilities;
        }

        private void HandleSkillStateChanged()
        {
            AvailabilityChanged?.Invoke();
        }
    }
}
