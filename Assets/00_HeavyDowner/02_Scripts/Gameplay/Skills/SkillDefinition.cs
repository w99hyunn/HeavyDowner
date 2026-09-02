using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public abstract class SkillDefinition : GameplayAbilityDefinition
    {
        [SerializeField] private Sprite icon;
        [SerializeField] private SkillCapability requiredFreeCapabilities;
        [SerializeField] private SkillCapability occupiedCapabilities;

        public Sprite Icon => icon;
        public SkillCapability RequiredFreeCapabilities => requiredFreeCapabilities;
        public SkillCapability OccupiedCapabilities => occupiedCapabilities;

        public abstract Awaitable ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken);

        public sealed override Awaitable ExecuteAsync(IGameplayAbilityContext context, int magnitude, CancellationToken cancellationToken)
        {
            return ExecuteAsync((SkillExecutionContext)context, cancellationToken);
        }
    }
}
