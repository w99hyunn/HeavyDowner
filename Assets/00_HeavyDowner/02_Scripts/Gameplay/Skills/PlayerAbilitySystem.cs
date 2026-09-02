using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class PlayerAbilitySystem : MonoBehaviour
    {
        [SerializeField] private SkillSlotDefinition[] slots;
        [SerializeField] private DescentBoardController board;

        private readonly Dictionary<SkillSlotId, SkillRuntime> skillsBySlot = new();
        private GameplayAbilitySystem abilitySystem;
        private IngamePlayerController player;
        private SkillCuePlayer cuePlayer;

        public event Action AvailabilityChanged;
        public event Action<SkillSlotId> CooldownStarted;

        private void Awake()
        {
            TryGetComponent<IngamePlayerController>(out player);
            TryGetComponent<SkillCuePlayer>(out cuePlayer);

            SkillExecutionContext context = new(player, board, cuePlayer);
            abilitySystem = new GameplayAbilitySystem(cuePlayer);
            foreach (SkillSlotDefinition slot in slots)
            {
                SkillRuntime runtime = abilitySystem.GrantAbility(new SkillRuntime(slot.Skill, context, destroyCancellationToken));
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
            SkillRuntime runtime = skillsBySlot[slotId];
            if (runtime.Definition.Cooldown <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(runtime.CooldownRemaining / runtime.Definition.Cooldown);
        }

        public bool CanActivate(SkillSlotId slotId)
        {
            SkillRuntime runtime = skillsBySlot[slotId];
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

            SkillRuntime runtime = skillsBySlot[slotId];
            runtime.Activate();
            if (runtime.Definition.Cooldown > 0f)
            {
                CooldownStarted?.Invoke(slotId);
            }
        }

        private SkillCapability GetOccupiedCapabilities()
        {
            SkillCapability occupiedCapabilities = SkillCapability.None;
            foreach (SkillRuntime runtime in skillsBySlot.Values)
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
