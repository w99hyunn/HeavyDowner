using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public interface IGameplayAbilityContext
    {
        SkillCuePlayer Cues { get; }
        Vector3 CuePosition { get; }
        Transform LoopCueAnchor { get; }
    }

    public abstract class GameplayAbilityDefinition : ScriptableObject
    {
        [SerializeField, Min(0f)] private float cooldown;
        [SerializeField] private SkillCueDefinition[] activationCues = Array.Empty<SkillCueDefinition>();
        [SerializeField] private SkillCueDefinition loopCue;
        [SerializeField] private SkillCueDefinition[] endCues = Array.Empty<SkillCueDefinition>();

        public float Cooldown => cooldown;
        public IReadOnlyList<SkillCueDefinition> ActivationCues => activationCues;
        public SkillCueDefinition LoopCue => loopCue;
        public IReadOnlyList<SkillCueDefinition> EndCues => endCues;

        public abstract Awaitable ExecuteAsync(IGameplayAbilityContext context, int magnitude, CancellationToken cancellationToken);

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
