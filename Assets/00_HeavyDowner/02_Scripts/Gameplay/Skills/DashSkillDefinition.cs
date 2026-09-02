using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Skills/Dash", fileName = "Dash")]
    public class DashSkillDefinition : SkillDefinition
    {
        [SerializeField, Min(1)] private int distance = 10;
        [SerializeField, Min(0)] private int halfWidth = 1;
        [SerializeField, Min(1)] private int damage = 800;
        [SerializeField, Min(0.01f)] private float stepInterval = 0.045f;
        [SerializeField, Min(1)] private int cueStepInterval = 3;
        [SerializeField] private SkillCueDefinition stepCue;

        public override async Awaitable ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
        {
            ISkillActor actor = context.Actor;
            Vector2Int origin = actor.CurrentCell;
            actor.SetMovementLocked(true);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                int travelDistance = context.Board.AttackCorridor(origin, distance, halfWidth, damage);
                context.Cues.PlayOneShot(stepCue, actor.SkillTransform.position);

                for (int step = 1; step <= travelDistance; step++)
                {
                    actor.MoveToCell(origin + Vector2Int.down * step);
                    if (step % cueStepInterval == 0)
                    {
                        context.Cues.PlayOneShot(stepCue, actor.SkillTransform.position);
                    }

                    await Awaitable.WaitForSecondsAsync(stepInterval, cancellationToken);
                }
            }
            finally
            {
                actor.SetMovementLocked(false);
            }
        }

        public override void CollectCues(List<SkillCueDefinition> cues)
        {
            base.CollectCues(cues);
            cues.Add(stepCue);
        }
    }
}
