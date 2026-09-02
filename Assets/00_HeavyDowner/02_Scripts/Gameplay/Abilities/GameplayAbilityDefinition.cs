using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public interface IGameplayAbilityContext
    {
        GameplayCuePlayer Cues { get; }
        Vector3 CuePosition { get; }
        Transform LoopCueAnchor { get; }
    }

    public abstract class GameplayAbilityDefinition : ScriptableObject
    {
        [SerializeField, Min(0f)] private float cooldown;
        [SerializeField] private GameplayCueDefinition[] activationCues = Array.Empty<GameplayCueDefinition>();
        [SerializeField] private GameplayCueDefinition loopCue;
        [SerializeField] private GameplayCueDefinition[] endCues = Array.Empty<GameplayCueDefinition>();

        public float Cooldown => cooldown;
        public IReadOnlyList<GameplayCueDefinition> ActivationCues => activationCues;
        public GameplayCueDefinition LoopCue => loopCue;
        public IReadOnlyList<GameplayCueDefinition> EndCues => endCues;

        public abstract Awaitable ExecuteAsync(IGameplayAbilityContext context, int magnitude, CancellationToken cancellationToken);

        public virtual void CollectCues(List<GameplayCueDefinition> cues)
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
