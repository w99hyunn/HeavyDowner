using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public abstract class BossAbilityDefinition : GameplayAbilityDefinition
    {
        public abstract Awaitable ExecuteAsync(BossAbilityContext context, int damage, CancellationToken cancellationToken);

        public sealed override async Awaitable ExecuteAsync(IGameplayAbilityContext context, int magnitude, CancellationToken cancellationToken)
        {
            BossAbilityContext bossContext = (BossAbilityContext)context;
            bossContext.BeginCast();
            try
            {
                await ExecuteAsync(bossContext, magnitude, cancellationToken);
            }
            finally
            {
                bossContext.EndCast();
            }
        }
    }
}
