using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HeavyDowner.Gameplay
{
    public readonly struct BoardActionResult
    {
        public BoardActionResult(bool canEnter, int counterDamage)
        {
            CanEnter = canEnter;
            CounterDamage = counterDamage;
        }

        public bool CanEnter { get; }
        public int CounterDamage { get; }
    }

    public sealed class DescentBoardController : MonoBehaviour, ISkillBoard
    {
        private const int MONSTER_BAND_HEIGHT = 4;

        [SerializeField] private Transform streamingCamera;
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private Tilemap enemyTilemap;

        [Header("Block Tiers")]
        [SerializeField, Min(1)] private int metersPerBlockTier = 50;
        [SerializeField] private TileBase[] blockTiles = new TileBase[10];
        [SerializeField] private int[] blockHealthByTier =
        {
            100, 100, 200, 200, 300,
            300, 400, 500, 600, 800
        };

        [Header("Enemies")]
        [SerializeField] private TileBase grubTile;
        [SerializeField] private TileBase batTile;
        [SerializeField] private TileBase golemTile;
        [SerializeField, Min(0f)] private float monsterHealthGrowthPerTier = 0.25f;

        [Header("Bosses")]
        [SerializeField] private BossDefinition[] bosses;
        [SerializeField, Min(1)] private int bossIntervalMeters = 50;

        [Header("Enhancement Orbs")]
        [SerializeField] private TileBase enhancementOrbTile;
        [SerializeField, Range(0f, 1f)] private float enhancementOrbSpawnChance = 0.04f;
        [SerializeField, Min(1)] private int enhancementOrbHealth = 100;
        [SerializeField, Min(1)] private int enhancementOrbAmount = 1;

        [Header("Streaming")]
        [SerializeField, Min(8)] private int chunkHeight = 32;
        [SerializeField, Min(1)] private int loadedChunkRadius = 1;

        [Header("Generation")]
        [SerializeField, Range(0f, 1f)] private float monsterSpawnChance = 0.45f;
        [SerializeField, Range(0f, 1f)] private float monster2x2Chance = 0.28f;
        [SerializeField, Range(0f, 1f)] private float monster4x4Chance = 0.08f;

        [Header("Damage")]
        [SerializeField, Min(0)] private int blockDamage = 100;
        [SerializeField, Range(0f, 1f)] private float minimumBlockDamageRatio = 0.7f;
        [SerializeField, Range(0f, 1f)] private float destroyedBlockDamageRatio = 0.35f;

        [Header("Presentation")]
        [SerializeField] private CellHealthBarPool healthBarPool;
        [SerializeField] private DamageTextPool damageTextPool;
        [SerializeField] private RunRewardSession rewardSession;
        [SerializeField] private BossAbilitySystem bossAbilitySystem;
        [SerializeField, Min(0f)] private float bossHitFlashDuration = 0.2f;

        private VerticalTilemapWorld world;
        private DestroyedCellVisualPool destroyedCellVisualPool;
        private TileHitFlashController hitFlashController;

        private readonly List<RowMutation> rowMutations = new();
        private readonly HashSet<int> loadedChunks = new();
        private readonly List<int> chunksToUnload = new();
        private readonly List<MonsterVisual> monsterVisuals = new();
        private readonly HashSet<Vector2Int> skillAttackAnchors = new();
        private readonly Dictionary<Vector2Int, int> remainingHealthByCell = new();
        private readonly Dictionary<Vector2Int, BossEntity> bossEntities = new();

        private TileBase[] terrainChunkTiles;
        private TileBase[] enemyChunkTiles;
        private int currentStreamingChunk = int.MaxValue;
        private int randomSeed;

        private enum CellType
        {
            Block,
            Enemy,
            Boss,
            EnhancementOrb
        }

        private readonly struct CellDefinition
        {
            public CellDefinition(CellType type, TileBase tile, int health, int counterDamage, Vector2Int anchorPosition, int footprintSize, BossDefinition boss = null)
            {
                Type = type;
                Tile = tile;
                Health = health;
                CounterDamage = counterDamage;
                AnchorPosition = anchorPosition;
                FootprintSize = footprintSize;
                Boss = boss;
            }

            public CellType Type { get; }
            public bool IsEnemy => Type == CellType.Enemy || Type == CellType.Boss;
            public bool IsBoss => Type == CellType.Boss;
            public bool IsEnhancementOrb => Type == CellType.EnhancementOrb;
            public TileBase Tile { get; }
            public int Health { get; }
            public int CounterDamage { get; }
            public Vector2Int AnchorPosition { get; }
            public int FootprintSize { get; }
            public BossDefinition Boss { get; }

            public bool Contains(Vector2Int position)
            {
                return position.x >= AnchorPosition.x
                    && position.x < AnchorPosition.x + FootprintSize
                    && position.y <= AnchorPosition.y
                    && position.y > AnchorPosition.y - FootprintSize;
            }
        }

        private readonly struct MonsterVisual
        {
            public MonsterVisual(Vector2Int position, int size)
            {
                Position = position;
                Size = size;
            }

            public Vector2Int Position { get; }
            public int Size { get; }
        }

        private struct RowMutation
        {
            public ushort DestroyedMask;
        }

        private void Awake()
        {
            TryGetComponent<VerticalTilemapWorld>(out world);
            TryGetComponent<DestroyedCellVisualPool>(out destroyedCellVisualPool);
            TryGetComponent<TileHitFlashController>(out hitFlashController);
        }

        private void Start()
        {
            randomSeed = UnityEngine.Random.Range(1, int.MaxValue);
            InitializeBoard();
        }

        private void Update()
        {
            StreamAround(world.WorldToCell(streamingCamera.position));
        }

        public BoardActionResult Attack(Vector2Int target, int damage)
        {
            if (!world.IsPlayableColumn(target.x))
            {
                return new BoardActionResult(false, 0);
            }

            if (!TryGetCellDefinition(target, out CellDefinition cell))
            {
                return new BoardActionResult(true, 0);
            }

            Vector2Int statePosition = cell.AnchorPosition;
            RowMutation mutation = GetRowMutation(statePosition.y);
            ushort cellMask = GetCellMask(statePosition.x);
            if ((mutation.DestroyedMask & cellMask) != 0)
            {
                return new BoardActionResult(true, 0);
            }

            if (cell.IsBoss)
            {
                return AttackBoss(cell, mutation, cellMask, damage);
            }

            int currentHealth = remainingHealthByCell.TryGetValue(statePosition, out int savedHealth)
                ? savedHealth
                : cell.Health;
            int remainingHealth = currentHealth - damage;
            int appliedDamage = Mathf.Min(damage, currentHealth);
            if (appliedDamage > 0)
            {
                damageTextPool.PlayWorldDamage(appliedDamage, GetCellVisualCenter(cell));
            }

            if (remainingHealth <= 0)
            {
                mutation.DestroyedMask |= cellMask;
                remainingHealthByCell.Remove(statePosition);
                SetRowMutation(statePosition.y, mutation);
                Vector3 visualCenter = GetCellVisualCenter(cell);
                ClearVisibleCell(cell);
                if (cell.IsEnemy)
                {
                    rewardSession.TryDropEquipment(visualCenter);
                }
                else if (cell.IsEnhancementOrb)
                {
                    rewardSession.CollectEnhancementOrb(enhancementOrbAmount, visualCenter);
                }

                int destructionDamage = cell.Type == CellType.Block
                    ? Mathf.CeilToInt(cell.CounterDamage * destroyedBlockDamageRatio)
                    : 0;
                return new BoardActionResult(true, destructionDamage);
            }

            remainingHealthByCell[statePosition] = remainingHealth;
            SetRowMutation(statePosition.y, mutation);
            ShowHealthBar(cell, remainingHealth);
            Tilemap tilemap = cell.IsEnemy ? enemyTilemap : terrainTilemap;
            int counterDamage = cell.CounterDamage;
            hitFlashController.Flash(tilemap, ToTilePosition(cell.AnchorPosition));

            if (cell.Type == CellType.Block)
            {
                float remainingHealthRatio = (float)remainingHealth / cell.Health;
                float damageRatio = Mathf.Lerp(minimumBlockDamageRatio, 1f, remainingHealthRatio);
                counterDamage = Mathf.CeilToInt(cell.CounterDamage * damageRatio);
            }
            return new BoardActionResult(false, counterDamage);
        }

        private BoardActionResult AttackBoss(CellDefinition cell, RowMutation mutation, ushort cellMask, int damage)
        {
            BossEntity boss = GetBossEntity(cell);
            BossAttackResult result = boss.ReceiveAttack(damage);
            if (result.AppliedDamage > 0)
            {
                damageTextPool.PlayWorldDamage(result.AppliedDamage, GetCellVisualCenter(cell));
            }

            if (result.IsDead)
            {
                mutation.DestroyedMask |= cellMask;
                bossEntities.Remove(cell.AnchorPosition);
                SetRowMutation(cell.AnchorPosition.y, mutation);
                Vector3 visualCenter = GetCellVisualCenter(cell);
                ClearVisibleCell(cell);
                rewardSession.DropEquipment(visualCenter);
                return new BoardActionResult(true, 0);
            }

            SetRowMutation(cell.AnchorPosition.y, mutation);
            ShowHealthBar(cell, result.RemainingHealth);
            if (result.FlashColor != default)
            {
                hitFlashController.Flash(enemyTilemap, ToTilePosition(cell.AnchorPosition), result.FlashColor, bossHitFlashDuration);
            }
            else
            {
                hitFlashController.Flash(enemyTilemap, ToTilePosition(cell.AnchorPosition));
            }

            return new BoardActionResult(false, result.CounterDamage);
        }

        private BossEntity GetBossEntity(CellDefinition cell)
        {
            if (bossEntities.TryGetValue(cell.AnchorPosition, out BossEntity boss))
            {
                return boss;
            }

            boss = bossAbilitySystem.CreateEntity(cell.Boss, cell.AnchorPosition, cell.Health);
            bossEntities.Add(cell.AnchorPosition, boss);
            return boss;
        }

        public void AttackCorridor(Vector2Int origin, int distance, int halfWidth, int damage)
        {
            skillAttackAnchors.Clear();

            for (int depth = 1; depth <= distance; depth++)
            {
                int row = origin.y - depth;
                for (int offset = -halfWidth; offset <= halfWidth; offset++)
                {
                    AttackSkillCell(new Vector2Int(origin.x + offset, row), damage);
                }
            }
        }

        public void AttackArea(Vector2Int center, int radius, int damage)
        {
            skillAttackAnchors.Clear();

            for (int y = -radius; y <= radius; y++)
            {
                int horizontalRadius = radius - Mathf.Abs(y);
                for (int x = -horizontalRadius; x <= horizontalRadius; x++)
                {
                    AttackSkillCell(center + new Vector2Int(x, y), damage);
                }
            }
        }

        public void AttackSplash(Vector2Int center, int radius, int damage)
        {
            skillAttackAnchors.Clear();
            if (TryGetCellDefinition(center, out CellDefinition centerCell))
            {
                skillAttackAnchors.Add(centerCell.AnchorPosition);
            }

            for (int y = -radius; y <= radius; y++)
            {
                int horizontalRadius = radius - Mathf.Abs(y);
                for (int x = -horizontalRadius; x <= horizontalRadius; x++)
                {
                    if (x != 0 || y != 0)
                    {
                        AttackSkillCell(center + new Vector2Int(x, y), damage);
                    }
                }
            }
        }

        private void AttackSkillCell(Vector2Int position, int damage)
        {
            if (!TryGetCellDefinition(position, out CellDefinition cell)
                || !skillAttackAnchors.Add(cell.AnchorPosition))
            {
                return;
            }

            Attack(cell.AnchorPosition, damage);
        }

        private void InitializeBoard()
        {
            terrainTilemap.ClearAllTiles();
            enemyTilemap.ClearAllTiles();
            rowMutations.Clear();
            remainingHealthByCell.Clear();
            foreach (BossEntity boss in bossEntities.Values)
            {
                boss.CancelAbility();
            }
            bossEntities.Clear();
            loadedChunks.Clear();
            chunksToUnload.Clear();
            hitFlashController.Clear();
            monsterVisuals.Clear();
            healthBarPool.HideAll();
            damageTextPool.HideAll();

            int boardWidth = world.HorizontalCellCount;
            terrainChunkTiles = new TileBase[boardWidth * chunkHeight];
            enemyChunkTiles = new TileBase[boardWidth * chunkHeight];
            currentStreamingChunk = int.MaxValue;

            StreamAround(world.WorldToCell(streamingCamera.position));
        }

        private void StreamAround(Vector2Int sourceCell)
        {
            int sourceChunk = GetChunkIndex(sourceCell.y);
            if (sourceChunk == currentStreamingChunk && loadedChunks.Count > 0)
            {
                return;
            }

            for (int chunk = sourceChunk - loadedChunkRadius; chunk <= sourceChunk + loadedChunkRadius; chunk++)
            {
                if (!loadedChunks.Contains(chunk))
                {
                    LoadChunk(chunk);
                }
            }

            chunksToUnload.Clear();
            foreach (int chunk in loadedChunks)
            {
                if (chunk < sourceChunk - loadedChunkRadius || chunk > sourceChunk + loadedChunkRadius)
                {
                    chunksToUnload.Add(chunk);
                }
            }

            for (int index = 0; index < chunksToUnload.Count; index++)
            {
                UnloadChunk(chunksToUnload[index]);
            }

            terrainTilemap.CompressBounds();
            enemyTilemap.CompressBounds();
            currentStreamingChunk = sourceChunk;
        }

        private void LoadChunk(int chunk)
        {
            Array.Clear(terrainChunkTiles, 0, terrainChunkTiles.Length);
            Array.Clear(enemyChunkTiles, 0, enemyChunkTiles.Length);
            monsterVisuals.Clear();

            int boardWidth = world.HorizontalCellCount;
            int halfWidth = boardWidth / 2;
            int firstRow = chunk * chunkHeight;

            for (int rowOffset = 0; rowOffset < chunkHeight; rowOffset++)
            {
                int row = firstRow + rowOffset;
                int boardRowStart = rowOffset * boardWidth;

                for (int columnOffset = 0; columnOffset < boardWidth; columnOffset++)
                {
                    int column = columnOffset - halfWidth;
                    Vector2Int position = new(column, row);
                    if (!TryGetCellDefinition(position, out CellDefinition cell))
                    {
                        continue;
                    }

                    RowMutation mutation = GetRowMutation(cell.AnchorPosition.y);
                    ushort cellMask = GetCellMask(cell.AnchorPosition.x);
                    if ((mutation.DestroyedMask & cellMask) != 0)
                    {
                        continue;
                    }

                    int tileIndex = boardRowStart + columnOffset;
                    if (cell.IsEnemy)
                    {
                        if (position != cell.AnchorPosition)
                        {
                            continue;
                        }

                        if (cell.IsBoss)
                        {
                            GetBossEntity(cell);
                        }

                        enemyChunkTiles[tileIndex] = cell.Tile;
                        monsterVisuals.Add(new MonsterVisual(cell.AnchorPosition, cell.FootprintSize));
                    }
                    else
                    {
                        terrainChunkTiles[tileIndex] = cell.Tile;
                    }
                }
            }

            BoundsInt boardBounds = new(-halfWidth, firstRow, 0, boardWidth, chunkHeight, 1);
            terrainTilemap.SetTilesBlock(boardBounds, terrainChunkTiles);
            enemyTilemap.SetTilesBlock(boardBounds, enemyChunkTiles);
            ApplyMonsterTransforms();
            loadedChunks.Add(chunk);
        }

        private void UnloadChunk(int chunk)
        {
            int boardWidth = world.HorizontalCellCount;
            int halfWidth = boardWidth / 2;
            int firstRow = chunk * chunkHeight;

            hitFlashController.RemoveRows(firstRow, chunkHeight);
            healthBarPool.HideRows(firstRow, chunkHeight);
            Array.Clear(terrainChunkTiles, 0, terrainChunkTiles.Length);
            Array.Clear(enemyChunkTiles, 0, enemyChunkTiles.Length);

            BoundsInt boardBounds = new(-halfWidth, firstRow, 0, boardWidth, chunkHeight, 1);
            terrainTilemap.SetTilesBlock(boardBounds, terrainChunkTiles);
            enemyTilemap.SetTilesBlock(boardBounds, enemyChunkTiles);
            loadedChunks.Remove(chunk);
        }

        private bool TryGetCellDefinition(Vector2Int position, out CellDefinition cell)
        {
            if (position.y >= 0 || !world.IsPlayableColumn(position.x))
            {
                cell = default;
                return false;
            }

            if (TryGetBossCell(position, out cell)
                || TryGetMonsterCell(position, out cell))
            {
                return true;
            }

            if (Random01(position, 0xA24BAED5u) < enhancementOrbSpawnChance)
            {
                cell = new CellDefinition(CellType.EnhancementOrb, enhancementOrbTile, enhancementOrbHealth, 0, position, 1);
                return true;
            }

            int blockTier = GetBlockTier(-position.y);
            cell = new CellDefinition(CellType.Block, blockTiles[blockTier], blockHealthByTier[blockTier], blockDamage, position, 1);
            return true;
        }

        private int GetBlockTier(int depth)
        {
            return Mathf.Min(blockTiles.Length - 1, (depth - 1) / metersPerBlockTier);
        }

        private bool TryGetMonsterCell(Vector2Int position, out CellDefinition cell)
        {
            int depth = -position.y;
            int band = (depth - 1) / MONSTER_BAND_HEIGHT;
            Vector2Int bandSeed = new(band, 0);
            if (Random01(bandSeed, 0x63D83595u) >= monsterSpawnChance)
            {
                cell = default;
                return false;
            }

            float sizeRoll = Random01(bandSeed, 0xC2B2AE35u);
            TileBase monsterTile;
            int monsterSize;
            int baseMonsterHealth;
            int monsterCounterDamage;

            if (sizeRoll < monster4x4Chance)
            {
                monsterTile = golemTile;
                monsterSize = 4;
                baseMonsterHealth = 300;
                monsterCounterDamage = 200;
            }
            else if (sizeRoll < monster4x4Chance + monster2x2Chance)
            {
                monsterTile = batTile;
                monsterSize = 2;
                baseMonsterHealth = 200;
                monsterCounterDamage = 100;
            }
            else
            {
                monsterTile = grubTile;
                monsterSize = 1;
                baseMonsterHealth = 100;
                monsterCounterDamage = 100;
            }

            int verticalRange = MONSTER_BAND_HEIGHT - monsterSize + 1;
            int verticalOffset = Mathf.Min(verticalRange - 1, Mathf.FloorToInt(Random01(bandSeed, 0x165667B1u) * verticalRange));
            int topRow = -(band * MONSTER_BAND_HEIGHT + 1 + verticalOffset);
            int monsterHealth = GetMonsterHealth(baseMonsterHealth, -topRow);

            int halfWidth = world.HorizontalCellCount / 2;
            int horizontalRange = world.HorizontalCellCount - monsterSize + 1;
            int leftColumn = -halfWidth + Mathf.Min(horizontalRange - 1, Mathf.FloorToInt(Random01(bandSeed, 0x27D4EB2Fu) * horizontalRange));

            cell = new CellDefinition(CellType.Enemy, monsterTile, monsterHealth, monsterCounterDamage, new Vector2Int(leftColumn, topRow), monsterSize);
            return cell.Contains(position);
        }

        private bool TryGetBossCell(Vector2Int position, out CellDefinition cell)
        {
            int depth = -position.y;
            int encounterNumber = depth / bossIntervalMeters;
            if (encounterNumber < 1)
            {
                cell = default;
                return false;
            }

            int bossDepth = encounterNumber * bossIntervalMeters;
            int bossSize = world.HorizontalCellCount;
            if (depth < bossDepth || depth >= bossDepth + bossSize)
            {
                cell = default;
                return false;
            }

            int bossIndex = (encounterNumber - 1) % bosses.Length;
            BossDefinition boss = bosses[bossIndex];
            int halfWidth = world.HorizontalCellCount / 2;

            cell = new CellDefinition(CellType.Boss, boss.IdleTile, GetMonsterHealth(boss.BaseHealth, bossDepth), boss.CounterDamage, new Vector2Int(-halfWidth, -bossDepth), bossSize, boss);
            return cell.Contains(position);
        }

        private int GetMonsterHealth(int baseHealth, int depth)
        {
            int depthTier = (depth - 1) / metersPerBlockTier;
            float healthMultiplier = 1f + depthTier * monsterHealthGrowthPerTier;
            return Mathf.CeilToInt(baseHealth * healthMultiplier);
        }

        private void ApplyMonsterTransforms()
        {
            for (int index = 0; index < monsterVisuals.Count; index++)
            {
                MonsterVisual visual = monsterVisuals[index];
                Vector3Int tilePosition = ToTilePosition(visual.Position);
                float offset = (visual.Size - 1) * 0.5f;
                Vector2 cellSize = world.CellSize;
                Matrix4x4 transformMatrix = Matrix4x4.TRS(new Vector3(offset * cellSize.x, -offset * cellSize.y), Quaternion.identity, new Vector3(visual.Size, visual.Size, 1f));
                enemyTilemap.SetTileFlags(tilePosition, TileFlags.None);
                enemyTilemap.SetTransformMatrix(tilePosition, transformMatrix);
            }
        }

        private RowMutation GetRowMutation(int row)
        {
            int index = GetRowMutationIndex(row);
            return index >= 0 && index < rowMutations.Count ? rowMutations[index] : default;
        }

        private void SetRowMutation(int row, RowMutation mutation)
        {
            int index = GetRowMutationIndex(row);
            while (rowMutations.Count <= index)
            {
                rowMutations.Add(default);
            }

            rowMutations[index] = mutation;
        }

        private static int GetRowMutationIndex(int row)
        {
            return -row - 1;
        }

        private int GetChunkIndex(int row)
        {
            return Mathf.FloorToInt((float)row / chunkHeight);
        }

        private ushort GetCellMask(int column)
        {
            int halfWidth = world.HorizontalCellCount / 2;
            return (ushort)(1 << column + halfWidth);
        }

        private float Random01(Vector2Int position, uint salt)
        {
            unchecked
            {
                uint value = (uint)randomSeed;
                value ^= (uint)position.x * 0x9E3779B9u;
                value ^= (uint)position.y * 0x85EBCA6Bu;
                value ^= salt;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (value & 0x00FFFFFFu) / 16777216f;
            }
        }

        private void ClearVisibleCell(CellDefinition cell)
        {
            if (!loadedChunks.Contains(GetChunkIndex(cell.AnchorPosition.y)))
            {
                return;
            }

            Tilemap tilemap = cell.IsEnemy ? enemyTilemap : terrainTilemap;
            Vector3Int tilePosition = ToTilePosition(cell.AnchorPosition);
            Sprite sprite = tilemap.GetSprite(tilePosition);
            PlayDestroyedCellVisual(cell, sprite);
            healthBarPool.Hide(cell.AnchorPosition);
            tilemap.SetTile(tilePosition, null);
            tilemap.SetColor(tilePosition, Color.white);
            hitFlashController.Remove(tilemap, tilePosition);
        }

        private void PlayDestroyedCellVisual(CellDefinition cell, Sprite sprite)
        {
            Vector3 scale = new(cell.FootprintSize, cell.FootprintSize, 1f);
            destroyedCellVisualPool.Play(sprite, GetCellVisualCenter(cell), scale);
        }

        private void ShowHealthBar(CellDefinition cell, int currentHealth)
        {
            float footprint = cell.FootprintSize;
            Vector2 cellSize = world.CellSize;
            Vector3 position = GetCellVisualCenter(cell)
                + Vector3.up * (footprint * cellSize.y * 0.38f);
            healthBarPool.Show(cell.AnchorPosition, position, footprint * cellSize.x * 0.78f, (float)currentHealth / cell.Health);
        }

        private Vector3 GetCellVisualCenter(CellDefinition cell)
        {
            float offset = (cell.FootprintSize - 1) * 0.5f;
            Vector2 cellSize = world.CellSize;
            return world.CellToWorld(cell.AnchorPosition) + new Vector3(offset * cellSize.x, -offset * cellSize.y);
        }

        private static Vector3Int ToTilePosition(Vector2Int position)
        {
            return new Vector3Int(position.x, position.y);
        }
    }
}
