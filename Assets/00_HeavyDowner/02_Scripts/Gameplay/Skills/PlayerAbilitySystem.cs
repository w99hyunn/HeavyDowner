using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public sealed class PlayerAbilitySystem : MonoBehaviour
    {
        [SerializeField] private SkillLoadoutDefinition loadout;
        [SerializeField] private DescentBoardController board;

        private readonly Dictionary<SkillSlotId, SkillRuntime> skillsBySlot = new();
        private readonly List<SkillRuntime> skillRuntimes = new();
        private IngamePlayerController player;
        private SkillCuePlayer cuePlayer;

        public event Action AvailabilityChanged;
        public event Action<SkillSlotId> CooldownStarted;

        private void Awake()
        {
            TryGetComponent<IngamePlayerController>(out player);
            TryGetComponent<SkillCuePlayer>(out cuePlayer);

            SkillExecutionContext context = new(player, board, cuePlayer);
            List<SkillCueDefinition> cues = new();
            foreach (SkillSlotDefinition slot in loadout.Slots)
            {
                slot.Skill.CollectCues(cues);
                foreach (SkillCueDefinition cue in cues)
                {
                    cuePlayer.Prewarm(cue);
                }

                cues.Clear();
                SkillRuntime runtime = new(
                    slot.Skill,
                    context,
                    destroyCancellationToken,
                    HandleSkillStateChanged);
                skillsBySlot.Add(slot.SlotId, runtime);
                skillRuntimes.Add(runtime);
            }
        }

        private void OnDisable()
        {
            foreach (SkillRuntime runtime in skillRuntimes)
            {
                runtime.Cancel();
            }
        }

        private void OnDestroy()
        {
            foreach (SkillRuntime runtime in skillRuntimes)
            {
                runtime.Dispose();
            }
        }

        public SkillDefinition GetSkillDefinition(SkillSlotId slotId)
        {
            return skillsBySlot[slotId].Definition;
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

            SkillCapability occupiedCapabilities = GetOccupiedCapabilities();
            return (runtime.Definition.RequiredFreeCapabilities & occupiedCapabilities) == SkillCapability.None;
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
            foreach (SkillRuntime runtime in skillRuntimes)
            {
                if (runtime.IsActive)
                {
                    occupiedCapabilities |= runtime.Definition.OccupiedCapabilities;
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
