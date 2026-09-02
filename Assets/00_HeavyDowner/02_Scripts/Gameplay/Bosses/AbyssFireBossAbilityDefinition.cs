using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Bosses/Abilities/Abyss Fire", fileName = "AbyssFire")]
    public class AbyssFireBossAbilityDefinition : BossAbilityDefinition
    {
        [SerializeField] private GameplayCueDefinition fireCue;
        [SerializeField] private GameplayCueDefinition breathCue;
        [SerializeField, Min(0f)] private float windup = 0.55f;
        [SerializeField, Min(0f)] private float damageDelay = 0.24f;
        [SerializeField, Min(0f)] private float aftermathDuration = 1.42f;
        [SerializeField] private Vector3 screenLocalPosition = new(0f, 0f, 10f);

        public override async Awaitable ExecuteAsync(BossAbilityContext context, int damage, CancellationToken cancellationToken)
        {
            await Awaitable.WaitForSecondsAsync(windup, cancellationToken);
            context.Cues.PlayOneShot(breathCue, context.CuePosition);
            context.Cues.PlayOneShot(fireCue, context.ScreenAnchor, screenLocalPosition);
            await Awaitable.WaitForSecondsAsync(damageDelay, cancellationToken);
            context.Player.TakeBossSkillDamage(damage);
            await Awaitable.WaitForSecondsAsync(aftermathDuration, cancellationToken);
        }

        public override void CollectCues(List<GameplayCueDefinition> cues)
        {
            base.CollectCues(cues);
            cues.Add(fireCue);
            cues.Add(breathCue);
        }
    }
}
