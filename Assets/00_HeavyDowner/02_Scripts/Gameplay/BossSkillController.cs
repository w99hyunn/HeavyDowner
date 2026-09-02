using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HeavyDowner.Gameplay
{
    public sealed class BossSkillController : MonoBehaviour
    {
        [SerializeField] private VerticalTilemapWorld world;
        [SerializeField] private Tilemap enemyTilemap;
        [SerializeField] private IngamePlayerController player;
        [SerializeField] private Transform streamingCamera;
        [SerializeField] private TileBase abyssBurrowerIdleTile;
        [SerializeField] private TileBase abyssBurrowerFireCastTile;
        [SerializeField] private TileBase crystalWardenIdleTile;
        [SerializeField] private TileBase crystalWardenLightningCastTile;
        [SerializeField] private GameObject abyssFireVfxPrefab;
        [SerializeField] private GameObject lightningWarningPrefab;
        [SerializeField] private GameObject lightningScreenVfxPrefab;
        [SerializeField] private GameObject lightningStrikePrefab;
        [SerializeField, Min(0f)] private float abyssFireWindup = 0.55f;
        [SerializeField, Min(0f)] private float abyssFireDuration = 1.1f;
        [SerializeField, Min(2)] private int abyssFireWaveStepCount = 7;
        [SerializeField, Min(0.01f)] private float abyssFireWaveStepInterval = 0.08f;
        [SerializeField, Min(0f)] private float lightningWarningDuration = 2f;
        [SerializeField, Min(0f)] private float lightningStrikeDuration = 0.45f;

        private readonly HashSet<Vector2Int> activeBossSkills = new();

        public bool TryPlayAbyssFire(Vector2Int bossAnchor, int damage)
        {
            if (!activeBossSkills.Add(bossAnchor))
            {
                return false;
            }

            _ = PlayAbyssFireAsync(bossAnchor, damage);
            return true;
        }

        public bool TryPlayCrystalLightning(Vector2Int bossAnchor, int damage)
        {
            if (!activeBossSkills.Add(bossAnchor))
            {
                return false;
            }

            _ = PlayCrystalLightningAsync(bossAnchor, damage);
            return true;
        }

        public void Cancel(Vector2Int bossAnchor)
        {
            activeBossSkills.Remove(bossAnchor);
        }

        private async Awaitable PlayAbyssFireAsync(Vector2Int bossAnchor, int damage)
        {
            Vector3Int tilePosition = ToTilePosition(bossAnchor);
            enemyTilemap.SetTile(tilePosition, abyssBurrowerFireCastTile);
            await Awaitable.WaitForSecondsAsync(abyssFireWindup);
            if (!activeBossSkills.Contains(bossAnchor))
            {
                return;
            }

            List<GameObject> fireEffects = new();
            bool damageApplied = false;
            for (int step = 0; step < abyssFireWaveStepCount; step++)
            {
                if (!activeBossSkills.Contains(bossAnchor))
                {
                    DestroyEffects(fireEffects);
                    return;
                }

                float progress = (float)step / (abyssFireWaveStepCount - 1);
                float waveY = Mathf.Lerp(-5.5f, 5.5f, progress);
                for (int column = -1; column <= 1; column++)
                {
                    GameObject fireVfx = Instantiate(abyssFireVfxPrefab, streamingCamera);
                    fireVfx.transform.localPosition = new Vector3(column * 3f, waveY, 10f);
                    fireVfx.transform.localRotation = abyssFireVfxPrefab.transform.localRotation;
                    fireEffects.Add(fireVfx);
                }

                if (!damageApplied && waveY >= 0f)
                {
                    player.TakeBossSkillDamage(damage);
                    damageApplied = true;
                }

                await Awaitable.WaitForSecondsAsync(abyssFireWaveStepInterval);
            }

            await Awaitable.WaitForSecondsAsync(abyssFireDuration);
            DestroyEffects(fireEffects);
            RestoreBossTile(tilePosition, abyssBurrowerFireCastTile, abyssBurrowerIdleTile);
            activeBossSkills.Remove(bossAnchor);
        }

        private async Awaitable PlayCrystalLightningAsync(Vector2Int bossAnchor, int damage)
        {
            Vector3Int bossTilePosition = ToTilePosition(bossAnchor);
            enemyTilemap.SetTile(bossTilePosition, crystalWardenLightningCastTile);

            GameObject screenVfx = Instantiate(lightningScreenVfxPrefab, streamingCamera);
            screenVfx.transform.localPosition = new Vector3(0f, 0f, 10f);
            screenVfx.transform.localRotation = lightningScreenVfxPrefab.transform.localRotation;

            Vector2Int[] targetCells = GetLightningTargetCells(bossAnchor);
            GameObject[] warnings = new GameObject[targetCells.Length];
            Vector3[] warningBaseScales = new Vector3[targetCells.Length];
            for (int index = 0; index < targetCells.Length; index++)
            {
                warnings[index] = Instantiate(
                    lightningWarningPrefab,
                    world.CellToWorld(targetCells[index]),
                    Quaternion.identity);
                warningBaseScales[index] = warnings[index].transform.localScale;
            }

            float warningStartTime = Time.time;
            while (Time.time - warningStartTime < lightningWarningDuration)
            {
                if (!activeBossSkills.Contains(bossAnchor))
                {
                    DestroyEffects(warnings);
                    Destroy(screenVfx);
                    return;
                }

                float phase = Mathf.PingPong((Time.time - warningStartTime) * 3f, 1f);
                float scale = Mathf.Lerp(0.78f, 1.05f, phase);
                for (int index = 0; index < warnings.Length; index++)
                {
                    warnings[index].transform.localScale = warningBaseScales[index] * scale;
                }

                await Awaitable.NextFrameAsync();
            }

            DestroyEffects(warnings);
            GameObject[] strikes = new GameObject[targetCells.Length];
            for (int index = 0; index < targetCells.Length; index++)
            {
                strikes[index] = Instantiate(
                    lightningStrikePrefab,
                    world.CellToWorld(targetCells[index]),
                    Quaternion.identity);
            }

            Vector2Int playerCell = player.CurrentCell;
            if (playerCell == targetCells[0] || playerCell == targetCells[1])
            {
                player.TakeBossSkillDamage(damage);
            }

            await Awaitable.WaitForSecondsAsync(lightningStrikeDuration);
            DestroyEffects(strikes);
            RestoreBossTile(
                bossTilePosition,
                crystalWardenLightningCastTile,
                crystalWardenIdleTile);
            activeBossSkills.Remove(bossAnchor);
        }

        private Vector2Int[] GetLightningTargetCells(Vector2Int bossAnchor)
        {
            int halfWidth = world.HorizontalCellCount / 2;
            int playerColumn = Mathf.Clamp(player.CurrentCell.x, -halfWidth, halfWidth);
            int secondColumn = Random.Range(-halfWidth, halfWidth + 1);
            while (secondColumn == playerColumn)
            {
                secondColumn = Random.Range(-halfWidth, halfWidth + 1);
            }

            int warningRow = bossAnchor.y + 1;
            return new[]
            {
                new Vector2Int(playerColumn, warningRow),
                new Vector2Int(secondColumn, warningRow)
            };
        }

        private void RestoreBossTile(
            Vector3Int position,
            TileBase castTile,
            TileBase idleTile)
        {
            if (enemyTilemap.GetTile(position) == castTile)
            {
                enemyTilemap.SetTile(position, idleTile);
            }
        }

        private static void DestroyEffects(IReadOnlyList<GameObject> effects)
        {
            for (int index = 0; index < effects.Count; index++)
            {
                Destroy(effects[index]);
            }
        }

        private static Vector3Int ToTilePosition(Vector2Int position)
        {
            return new Vector3Int(position.x, position.y);
        }
    }
}
