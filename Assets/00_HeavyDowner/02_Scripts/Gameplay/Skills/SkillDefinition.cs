using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public abstract class SkillDefinition : ScriptableObject
    {
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0f)] private float cooldown;
        [SerializeField] private SkillCapability requiredFreeCapabilities;
        [SerializeField] private SkillCapability occupiedCapabilities;
        [SerializeField] private SkillCueDefinition[] activationCues;
        [SerializeField] private SkillCueDefinition loopCue;
        [SerializeField] private SkillCueDefinition[] endCues;

        public Sprite Icon => icon;
        public float Cooldown => cooldown;
        public SkillCapability RequiredFreeCapabilities => requiredFreeCapabilities;
        public SkillCapability OccupiedCapabilities => occupiedCapabilities;
        public IReadOnlyList<SkillCueDefinition> ActivationCues => activationCues;
        public SkillCueDefinition LoopCue => loopCue;
        public IReadOnlyList<SkillCueDefinition> EndCues => endCues;

        public abstract Awaitable ExecuteAsync(
            SkillExecutionContext context,
            CancellationToken cancellationToken);

        public virtual void CollectCues(List<SkillCueDefinition> cues)
        {
            cues.AddRange(activationCues);
            if (loopCue != null)
            {
                cues.Add(loopCue);
            }
            cues.AddRange(endCues);
        }
    }
}
