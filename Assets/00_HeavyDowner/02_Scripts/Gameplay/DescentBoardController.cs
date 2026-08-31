using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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

    public sealed class DescentBoardController : MonoBehaviour
    {
        private const int MONSTER_BAND_HEIGHT = 4;

        [SerializeField] private VerticalTilemapWorld world;
        [FormerlySerializedAs("rewindCamera")]
        [SerializeField] private Transform streamingCamera;
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private Tilemap enemyTilemap;

        [Header("Block Tiers")]
        [SerializeField, Min(1)] private int metersPerBlockTier = 50;
        [SerializeField] private TileBase[] blockTiles = new TileBase[10];
        [SerializeField] private int[] blockHealthByTier = { 1, 1, 2, 2, 3, 3, 4, 5, 6, 8 };

        [Header("Enemy Tiles")]
        [SerializeField] private TileBase grubTile;
        [SerializeField] private TileBase batTile;
        [SerializeField] private TileBase golemTile;

        [Header("Streaming")]
        [SerializeField, Min(8)] private int chunkHeight = 32;
        [SerializeField, Min(1)] private int loadedChunkRadius = 1;

        [Header("Generation")]
        [SerializeField] private int randomSeed = 1655;
        [SerializeField, Range(0f, 1f)] private float monsterSpawnChance = 0.45f;
        [SerializeField, Range(0f, 1f)] private float monster2x2Chance = 0.28f;
        [SerializeField, Range(0f, 1f)] private float monster4x4Chance = 0.08f;

        [Header("Damage")]
        [SerializeField, Min(0)] private int blockDamage = 1;
        [SerializeField] private float hitFlashDuration = 0.08f;
        [SerializeField] private Color hitColor = new(1f, 0.72f, 0.62f, 1f);

        private readonly List<RowMutation> rowMutations = new();
        private readonly HashSet<int> loadedChunks = new();
        private readonly List<int> chunksToUnload = new();
        private readonly List<HitFlash> hitFlashes = new();
        private readonly List<MonsterVisual> monsterVisuals = new();

        private TileBase[] terrainChunkTiles;
        private TileBase[] enemyChunkTiles;
        private int currentStreamingChunk = int.MaxValue;

        public int RecordedRowCount => rowMutations.Count;
        public int LoadedChunkCount => loadedChunks.Count;

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
            public ulong RemainingHealth;
        }

        private struct HitFlash
        {
            public HitFlash(Tilemap tilemap, Vector3Int position, float restoreTime)
            {
                Tilemap = tilemap;
                Position = position;
                RestoreTime = restoreTime;
            }

            public Tilemap Tilemap { get; }
            public Vector3Int Position { get; }
            public float RestoreTime { get; set; }
        }

        private void Start()
        {
            InitializeBoard();
        }

        private void Update()
        {
            RestoreHitFlashes();
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
                ? GetRemainingHealth(mutation, statePosition.x)
                : cell.Health;
            int remainingHealth = currentHealth - damage;

            if (remainingHealth <= 0)
            {
                mutation.DestroyedMask |= cellMask;
                mutation.DamagedMask &= (ushort)~cellMask;
                mutation.RemainingHealth = SetRemainingHealth(mutation.RemainingHealth, statePosition.x, 0);
                SetRowMutation(statePosition.y, mutation);
                ClearVisibleCell(cell);
                int destructionDamage = cell.IsEnemy ? 0 : cell.CounterDamage;
                return new BoardActionResult(true, destructionDamage);
            }

            mutation.DamagedMask |= cellMask;
            mutation.RemainingHealth = SetRemainingHealth(mutation.RemainingHealth, statePosition.x, remainingHealth);
            SetRowMutation(statePosition.y, mutation);
            FlashCell(cell, cell.IsEnemy ? enemyTilemap : terrainTilemap);
            return new BoardActionResult(false, cell.CounterDamage);
        }

        private void InitializeBoard()
        {
            terrainTilemap.ClearAllTiles();
            enemyTilemap.ClearAllTiles();
            rowMutations.Clear();
            loadedChunks.Clear();
            chunksToUnload.Clear();
            hitFlashes.Clear();
            monsterVisuals.Clear();

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

            RemoveFlashesInChunk(firstRow);
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
                health = 3;
                counterDamage = 2;
            }
            else if (sizeRoll < monster4x4Chance + monster2x2Chance)
            {
                size = 2;
                kind = CellKind.Bat;
                health = 2;
                counterDamage = 1;
            }
            else
            {
                size = 1;
                kind = CellKind.Grub;
                health = 1;
                counterDamage = 1;
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

        private int GetRemainingHealth(RowMutation mutation, int column)
        {
            int halfWidth = world.HorizontalCellCount / 2;
            int shift = (column + halfWidth) * 4;
            return (int)(mutation.RemainingHealth >> shift & 0xFUL);
        }

        private ulong SetRemainingHealth(ulong packedHealth, int column, int health)
        {
            int halfWidth = world.HorizontalCellCount / 2;
            int shift = (column + halfWidth) * 4;
            ulong cellBits = 0xFUL << shift;
            return packedHealth & ~cellBits | (ulong)health << shift;
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
            tilemap.SetTile(tilePosition, null);
            tilemap.SetColor(tilePosition, Color.white);
            RemoveFlash(tilemap, tilePosition);
        }

        private void FlashCell(CellDefinition cell, Tilemap tilemap)
        {
            if (!loadedChunks.Contains(GetChunkIndex(cell.AnchorPosition.y)))
            {
                return;
            }

            Vector3Int tilePosition = ToTilePosition(cell.AnchorPosition);
            tilemap.SetTileFlags(tilePosition, TileFlags.None);
            tilemap.SetColor(tilePosition, hitColor);

            for (int index = 0; index < hitFlashes.Count; index++)
            {
                HitFlash flash = hitFlashes[index];
                if (flash.Tilemap == tilemap && flash.Position == tilePosition)
                {
                    flash.RestoreTime = Time.time + hitFlashDuration;
                    hitFlashes[index] = flash;
                    return;
                }
            }

            hitFlashes.Add(new HitFlash(tilemap, tilePosition, Time.time + hitFlashDuration));
        }

        private void RestoreHitFlashes()
        {
            for (int index = hitFlashes.Count - 1; index >= 0; index--)
            {
                HitFlash flash = hitFlashes[index];
                if (Time.time < flash.RestoreTime)
                {
                    continue;
                }

                flash.Tilemap.SetColor(flash.Position, Color.white);
                hitFlashes.RemoveAt(index);
            }
        }

        private void RemoveFlash(Tilemap tilemap, Vector3Int position)
        {
            for (int index = hitFlashes.Count - 1; index >= 0; index--)
            {
                HitFlash flash = hitFlashes[index];
                if (flash.Tilemap == tilemap && flash.Position == position)
                {
                    hitFlashes.RemoveAt(index);
                }
            }
        }

        private void RemoveFlashesInChunk(int firstRow)
        {
            int lastRow = firstRow + chunkHeight;
            for (int index = hitFlashes.Count - 1; index >= 0; index--)
            {
                HitFlash flash = hitFlashes[index];
                if (flash.Position.y < firstRow || flash.Position.y >= lastRow)
                {
                    continue;
                }

                flash.Tilemap.SetColor(flash.Position, Color.white);
                hitFlashes.RemoveAt(index);
            }
        }

        private static Vector3Int ToTilePosition(Vector2Int position)
        {
            return new Vector3Int(position.x, position.y);
        }
    }
}
