using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Bosses/Abilities/Abyss Fire", fileName = "AbyssFire")]
    public sealed class AbyssFireBossAbilityDefinition : BossAbilityDefinition
    {
        [SerializeField] private SkillCueDefinition fireCue;
        [SerializeField, Min(0f)] private float windup = 0.55f;
        [SerializeField, Min(0f)] private float aftermathDuration = 1.1f;
        [SerializeField, Min(2)] private int waveStepCount = 7;
        [SerializeField, Min(0.01f)] private float waveStepInterval = 0.08f;
        [SerializeField] private Vector2 verticalRange = new(-5.5f, 5.5f);
        [SerializeField, Min(1)] private int columnCount = 3;
        [SerializeField, Min(0f)] private float columnSpacing = 3f;
        [SerializeField] private float screenPlaneOffset = 10f;

        public override async Awaitable ExecuteAsync(BossAbilityContext context, int damage, CancellationToken cancellationToken)
        {
            await Awaitable.WaitForSecondsAsync(windup, cancellationToken);

            bool damageApplied = false;
            for (int step = 0; step < waveStepCount; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                float progress = (float)step / (waveStepCount - 1);
                float waveY = Mathf.Lerp(verticalRange.x, verticalRange.y, progress);
                float firstColumn = (columnCount - 1) * -0.5f;

                for (int column = 0; column < columnCount; column++)
                {
                    float waveX = (firstColumn + column) * columnSpacing;
                    context.Cues.PlayOneShot(fireCue, context.ScreenAnchor, new Vector3(waveX, waveY, screenPlaneOffset));
                }

                if (!damageApplied && waveY >= 0f)
                {
                    context.Player.TakeBossSkillDamage(damage);
                    damageApplied = true;
                }

                await Awaitable.WaitForSecondsAsync(waveStepInterval, cancellationToken);
            }

            await Awaitable.WaitForSecondsAsync(aftermathDuration, cancellationToken);
        }

        public override void CollectCues(List<SkillCueDefinition> cues)
        {
            base.CollectCues(cues);
            cues.Add(fireCue);
        }
    }
}
