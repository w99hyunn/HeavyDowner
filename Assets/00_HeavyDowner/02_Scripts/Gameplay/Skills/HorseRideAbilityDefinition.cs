using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Abilities/Horse Ride", fileName = "HorseRide")]
    public class HorseRideAbilityDefinition : SkillDefinition
    {
        [SerializeField] private GameplayCueDefinition attackCue;
        [SerializeField] private GameplayCueDefinition movementCue;

        public override async Awaitable ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
        {
            ISkillActor actor = context.Actor;
            Vector2Int horizontalDirection = Vector2Int.right;
            float endTime = Time.time + 3f;
            int movementStep = 0;
            actor.SetMovementLocked(true);

            try
            {
                while (Time.time < endTime && !actor.IsDead)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Vector2Int horizontalTarget = actor.CurrentCell + horizontalDirection;
                    if (context.Board.IsPlayableColumn(horizontalTarget.x))
                    {
                        if (!TryMove(context, horizontalTarget, movementStep++ % 2 == 0))
                        {
                            return;
                        }

                        await Awaitable.WaitForSecondsAsync(0.06f, cancellationToken);
                        continue;
                    }

                    for (int step = 0; step < 2 && Time.time < endTime; step++)
                    {
                        if (!TryMove(context, actor.CurrentCell + Vector2Int.down, movementStep++ % 2 == 0))
                        {
                            return;
                        }

                        await Awaitable.WaitForSecondsAsync(0.06f, cancellationToken);
                    }

                    horizontalDirection = -horizontalDirection;
                }
            }
            finally
            {
                actor.SetMovementLocked(false);
            }
        }

        private bool TryMove(SkillExecutionContext context, Vector2Int target, bool playMovementCue)
        {
            if (!context.Board.TryClearTraversalCell(target, out bool didAttack))
            {
                return false;
            }

            Vector3 previousPosition = context.CuePosition;
            context.Actor.SetAbilityMovementDirection(target - context.Actor.CurrentCell);
            context.Actor.MoveToCell(target);
            if (playMovementCue)
            {
                context.Cues.PlayOneShot(movementCue, previousPosition);
            }
            if (didAttack)
            {
                context.Cues.PlayOneShot(attackCue, context.CuePosition);
            }
            return true;
        }

        public override void CollectCues(List<GameplayCueDefinition> cues)
        {
            base.CollectCues(cues);
            cues.Add(attackCue);
            cues.Add(movementCue);
        }
    }
}
