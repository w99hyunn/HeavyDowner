using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Skills/Shield", fileName = "Shield")]
    public sealed class ShieldSkillDefinition : SkillDefinition
    {
        [SerializeField, Range(0f, 1f)] private float damageReduction = 0.5f;
        [SerializeField, Range(0f, 1f)] private float healthNormalized = 0.25f;
        [SerializeField, Min(0.1f)] private float duration = 5f;

        public override async Awaitable ExecuteAsync(
            SkillExecutionContext context,
            CancellationToken cancellationToken)
        {
            ISkillActor actor = context.Actor;
            bool depleted = false;

            void HandleShieldDepleted()
            {
                depleted = true;
            }

            actor.ShieldDepleted += HandleShieldDepleted;
            actor.ActivateShield(damageReduction, healthNormalized);
            float endTime = Time.time + duration;

            try
            {
                while (!depleted && Time.time < endTime)
                {
                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }
            finally
            {
                actor.ShieldDepleted -= HandleShieldDepleted;
                if (!depleted)
                {
                    actor.DeactivateShield();
                }
            }
        }
    }
}
