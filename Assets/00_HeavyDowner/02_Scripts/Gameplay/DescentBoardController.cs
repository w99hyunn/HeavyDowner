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
        [SerializeField] private int[] blockHealthByTier = { 100, 100, 200, 200, 300, 300, 400, 500, 600, 800 };

        [Header("Enemy Tiles")]
        [SerializeField] private TileBase grubTile;
        [SerializeField] private TileBase batTile;
        [SerializeField] private TileBase golemTile;

        [Header("Streaming")]
        [SerializeField, Min(8)] private int chunkHeight = 32;
        [SerializeField, Min(1)] private int loadedChunkRadius = 1;

        [Header("Generation")]
        [SerializeField, Range(0f, 1f)] private float monsterSpawnChance = 0.45f;
        [SerializeField, Range(0f, 1f)] private float monster2x2Chance = 0.28f;
        [SerializeField, Range(0f, 1f)] private float monster4x4Chance = 0.08f;

        [Header("Damage")]
        [SerializeField, Min(0)] private int blockDamage = 100;

        [Header("Presentation")]
        [SerializeField] private CellHealthBarPool healthBarPool;
        [SerializeField] private DamageTextPool damageTextPool;

        private VerticalTilemapWorld world;
        private DestroyedCellVisualPool destroyedCellVisualPool;
        private TileHitFlashController hitFlashController;

        private readonly List<RowMutation> rowMutations = new();
        private readonly HashSet<int> loadedChunks = new();
        private readonly List<int> chunksToUnload = new();
        private readonly List<MonsterVisual> monsterVisuals = new();
        private readonly HashSet<Vector2Int> skillAttackAnchors = new();
        private readonly Dictionary<Vector2Int, int> remainingHealthByCell = new();

        private TileBase[] terrainChunkTiles;
        private TileBase[] enemyChunkTiles;
        private int currentStreamingChunk = int.MaxValue;
        private int randomSeed;

        private enum CellKind
        {
            Block,
            Grub,
            Bat,
            Golem
        }

        private readonly struct CellDefinition
        {
            public CellDefinition(
                CellKind kind,
                int health,
                int counterDamage,
                Vector2Int anchorPosition,
                int footprintSize,
                int blockTier)
            {
                Kind = kind;
                Health = health;
                CounterDamage = counterDamage;
                AnchorPosition = anchorPosition;
                FootprintSize = footprintSize;
                BlockTier = blockTier;
            }

            public CellKind Kind { get; }
            public int Health { get; }
            public int CounterDamage { get; }
            public Vector2Int AnchorPosition { get; }
            public int FootprintSize { get; }
            public int BlockTier { get; }
            public bool IsEnemy => Kind is CellKind.Grub or CellKind.Bat or CellKind.Golem;
        }

        private readonly struct MonsterPlacement
        {
            public MonsterPlacement(
                CellKind kind,
                int health,
                int counterDamage,
                Vector2Int anchorPosition,
                int size)
            {
                Kind = kind;
                Health = health;
                CounterDamage = counterDamage;
                AnchorPosition = anchorPosition;
                Size = size;
            }

            public CellKind Kind { get; }
            public int Health { get; }
            public int CounterDamage { get; }
            public Vector2Int AnchorPosition { get; }
            public int Size { get; }

            public bool Contains(Vector2Int position)
            {
                return position.x >= AnchorPosition.x
                    && position.x < AnchorPosition.x + Size
                    && position.y <= AnchorPosition.y
                    && position.y > AnchorPosition.y - Size;
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
            public ushort DamagedMask;
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

            int currentHealth = (mutation.DamagedMask & cellMask) != 0
                ? remainingHealthByCell[statePosition]
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
                mutation.DamagedMask &= (ushort)~cellMask;
                remainingHealthByCell.Remove(statePosition);
                SetRowMutation(statePosition.y, mutation);
                ClearVisibleCell(cell);
                int destructionDamage = cell.IsEnemy ? 0 : cell.CounterDamage;
                return new BoardActionResult(true, destructionDamage);
            }

            mutation.DamagedMask |= cellMask;
            remainingHealthByCell[statePosition] = remainingHealth;
            SetRowMutation(statePosition.y, mutation);
            ShowHealthBar(cell, remainingHealth);
            Tilemap tilemap = cell.IsEnemy ? enemyTilemap : terrainTilemap;
            hitFlashController.Flash(tilemap, ToTilePosition(cell.AnchorPosition));
            return new BoardActionResult(false, cell.CounterDamage);
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

                        enemyChunkTiles[tileIndex] = GetTile(cell);
                        monsterVisuals.Add(new MonsterVisual(cell.AnchorPosition, cell.FootprintSize));
                    }
                    else
                    {
                        terrainChunkTiles[tileIndex] = GetTile(cell);
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

            if (TryGetMonsterPlacement(position, out MonsterPlacement monster))
            {
                cell = new CellDefinition(
                    monster.Kind,
                    monster.Health,
                    monster.CounterDamage,
                    monster.AnchorPosition,
                    monster.Size,
                    -1);
                return true;
            }

            int blockTier = GetBlockTier(-position.y);
            cell = new CellDefinition(
                CellKind.Block,
                blockHealthByTier[blockTier],
                blockDamage,
                position,
                1,
                blockTier);
            return true;
        }

        private int GetBlockTier(int depth)
        {
            return Mathf.Min(blockTiles.Length - 1, (depth - 1) / metersPerBlockTier);
        }

        private bool TryGetMonsterPlacement(Vector2Int position, out MonsterPlacement monster)
        {
            int depth = -position.y;
            int band = (depth - 1) / MONSTER_BAND_HEIGHT;
            Vector2Int bandSeed = new(band, 0);
            if (Random01(bandSeed, 0x63D83595u) >= monsterSpawnChance)
            {
                monster = default;
                return false;
            }

            float sizeRoll = Random01(bandSeed, 0xC2B2AE35u);
            int size;
            CellKind kind;
            int health;
            int counterDamage;

            if (sizeRoll < monster4x4Chance)
            {
                size = 4;
                kind = CellKind.Golem;
                health = 300;
                counterDamage = 200;
            }
            else if (sizeRoll < monster4x4Chance + monster2x2Chance)
            {
                size = 2;
                kind = CellKind.Bat;
                health = 200;
                counterDamage = 100;
            }
            else
            {
                size = 1;
                kind = CellKind.Grub;
                health = 100;
                counterDamage = 100;
            }

            int verticalRange = MONSTER_BAND_HEIGHT - size + 1;
            int verticalOffset = Mathf.Min(
                verticalRange - 1,
                Mathf.FloorToInt(Random01(bandSeed, 0x165667B1u) * verticalRange));
            int topRow = -(band * MONSTER_BAND_HEIGHT + 1 + verticalOffset);

            int halfWidth = world.HorizontalCellCount / 2;
            int horizontalRange = world.HorizontalCellCount - size + 1;
            int leftColumn = -halfWidth + Mathf.Min(
                horizontalRange - 1,
                Mathf.FloorToInt(Random01(bandSeed, 0x27D4EB2Fu) * horizontalRange));

            monster = new MonsterPlacement(
                kind,
                health,
                counterDamage,
                new Vector2Int(leftColumn, topRow),
                size);
            return monster.Contains(position);
        }

        private TileBase GetTile(CellDefinition cell)
        {
            return cell.Kind switch
            {
                CellKind.Block => blockTiles[cell.BlockTier],
                CellKind.Grub => grubTile,
                CellKind.Bat => batTile,
                CellKind.Golem => golemTile,
                _ => blockTiles[0]
            };
        }

        private void ApplyMonsterTransforms()
        {
            for (int index = 0; index < monsterVisuals.Count; index++)
            {
                MonsterVisual visual = monsterVisuals[index];
                Vector3Int tilePosition = ToTilePosition(visual.Position);
                float offset = (visual.Size - 1) * 0.5f;
                Vector2 cellSize = world.CellSize;
                Matrix4x4 transformMatrix = Matrix4x4.TRS(
                    new Vector3(offset * cellSize.x, -offset * cellSize.y),
                    Quaternion.identity,
                    new Vector3(visual.Size, visual.Size, 1f));
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
            healthBarPool.Show(
                cell.AnchorPosition,
                position,
                footprint * cellSize.x * 0.78f,
                (float)currentHealth / cell.Health);
        }

        private Vector3 GetCellVisualCenter(CellDefinition cell)
        {
            float offset = (cell.FootprintSize - 1) * 0.5f;
            Vector2 cellSize = world.CellSize;
            return world.CellToWorld(cell.AnchorPosition) + new Vector3(
                offset * cellSize.x,
                -offset * cellSize.y);
        }

        private static Vector3Int ToTilePosition(Vector2Int position)
        {
            return new Vector3Int(position.x, position.y);
        }
    }
}
