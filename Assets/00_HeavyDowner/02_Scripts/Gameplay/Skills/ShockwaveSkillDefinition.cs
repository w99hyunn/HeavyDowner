using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Skills/Shockwave", fileName = "Shockwave")]
    public class ShockwaveSkillDefinition : SkillDefinition
    {
        [SerializeField, Min(1)] private int radius = 2;
        [SerializeField, Min(1)] private int damage = 300;

        public override async Awaitable ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
        {
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            context.Board.AttackArea(context.Actor.CurrentCell, radius, damage);
        }
    }
}
