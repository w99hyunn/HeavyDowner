using System;
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

        public virtual bool RunsAsynchronously => false;

        public virtual void Execute(SkillExecutionContext context)
        {
            throw new NotSupportedException($"{name} must implement synchronous execution.");
        }

        public virtual Awaitable ExecuteAsync(
            SkillExecutionContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException($"{name} must implement asynchronous execution.");
        }

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
