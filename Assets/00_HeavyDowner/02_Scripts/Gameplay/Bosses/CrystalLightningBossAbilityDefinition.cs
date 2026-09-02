using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Bosses/Abilities/Crystal Lightning", fileName = "CrystalLightning")]
    public sealed class CrystalLightningBossAbilityDefinition : BossAbilityDefinition
    {
        [SerializeField] private SkillCueDefinition warningCue;
        [SerializeField] private SkillCueDefinition screenCue;
        [SerializeField] private SkillCueDefinition strikeCue;
        [SerializeField, Min(0f)] private float warningDuration = 2f;
        [SerializeField, Min(0f)] private float strikeDuration = 0.45f;
        [SerializeField] private Vector2 warningScaleRange = new(0.78f, 1.05f);
        [SerializeField, Min(0f)] private float warningPulseSpeed = 3f;
        [SerializeField] private Vector3 screenLocalPosition = new(0f, 0f, 10f);

        public override async Awaitable ExecuteAsync(BossAbilityContext context, int damage, CancellationToken cancellationToken)
        {
            context.Cues.PlayOneShot(screenCue, context.ScreenAnchor, screenLocalPosition);
            Vector2Int[] targetCells = GetTargetCells(context);
            SkillCueHandle[] warnings = new SkillCueHandle[targetCells.Length];
            for (int index = 0; index < targetCells.Length; index++)
            {
                warnings[index] = context.Cues.PlayOneShot(warningCue, context.World.CellToWorld(targetCells[index]));
            }

            try
            {
                float warningStartTime = Time.time;
                while (Time.time - warningStartTime < warningDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    float phase = Mathf.PingPong((Time.time - warningStartTime) * warningPulseSpeed, 1f);
                    float scale = Mathf.Lerp(warningScaleRange.x, warningScaleRange.y, phase);
                    for (int index = 0; index < warnings.Length; index++)
                    {
                        context.Cues.SetScale(warnings[index], scale);
                    }

                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }
            finally
            {
                for (int index = 0; index < warnings.Length; index++)
                {
                    context.Cues.Stop(warnings[index]);
                }
            }

            for (int index = 0; index < targetCells.Length; index++)
            {
                context.Cues.PlayOneShot(strikeCue, context.World.CellToWorld(targetCells[index]));
            }

            Vector2Int playerCell = context.Player.CurrentCell;
            if (playerCell == targetCells[0] || playerCell == targetCells[1])
            {
                context.Player.TakeBossSkillDamage(damage);
            }

            await Awaitable.WaitForSecondsAsync(strikeDuration, cancellationToken);
        }

        public override void CollectCues(List<SkillCueDefinition> cues)
        {
            base.CollectCues(cues);
            cues.Add(warningCue);
            cues.Add(screenCue);
            cues.Add(strikeCue);
        }

        private static Vector2Int[] GetTargetCells(BossAbilityContext context)
        {
            int halfWidth = context.World.HorizontalCellCount / 2;
            int playerColumn = Mathf.Clamp(context.Player.CurrentCell.x, -halfWidth, halfWidth);
            int secondColumn = Random.Range(-halfWidth, halfWidth + 1);
            while (secondColumn == playerColumn)
            {
                secondColumn = Random.Range(-halfWidth, halfWidth + 1);
            }

            int warningRow = context.Entity.Anchor.y + 1;
            return new[]
            {
                new Vector2Int(playerColumn, warningRow),
                new Vector2Int(secondColumn, warningRow)
            };
        }
    }
}
