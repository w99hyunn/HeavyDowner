using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Skills/Shockwave", fileName = "Shockwave")]
    public sealed class ShockwaveSkillDefinition : SkillDefinition
    {
        [SerializeField, Min(1)] private int radius = 2;
        [SerializeField, Min(1)] private int damage = 300;

        public override void Execute(SkillExecutionContext context)
        {
            context.Board.AttackArea(context.Actor.CurrentCell, radius, damage);
        }
    }
}
