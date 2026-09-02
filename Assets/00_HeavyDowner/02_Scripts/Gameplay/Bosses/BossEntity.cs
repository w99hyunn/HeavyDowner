using UnityEngine;
using UnityEngine.Tilemaps;

namespace HeavyDowner.Gameplay
{
    public readonly struct BossAttackResult
    {
        public BossAttackResult(int appliedDamage, int remainingHealth, int counterDamage, Color flashColor)
        {
            AppliedDamage = appliedDamage;
            RemainingHealth = remainingHealth;
            CounterDamage = counterDamage;
            FlashColor = flashColor;
        }

        public int AppliedDamage { get; }
        public int RemainingHealth { get; }
        public int CounterDamage { get; }
        public Color FlashColor { get; }
        public bool IsDead => RemainingHealth <= 0;
    }

    public readonly struct BossAbilityContext : IGameplayAbilityContext
    {
        public BossAbilityContext(BossEntity entity, VerticalTilemapWorld world, Tilemap enemyTilemap, IngamePlayerController player, Transform screenAnchor, GameplayCuePlayer cues)
        {
            Entity = entity;
            World = world;
            EnemyTilemap = enemyTilemap;
            Player = player;
            ScreenAnchor = screenAnchor;
            Cues = cues;
        }

        public BossEntity Entity { get; }
        public VerticalTilemapWorld World { get; }
        public Tilemap EnemyTilemap { get; }
        public IngamePlayerController Player { get; }
        public Transform ScreenAnchor { get; }
        public GameplayCuePlayer Cues { get; }
        public Vector3 CuePosition => World.CellToWorld(Entity.Anchor);
        public Transform LoopCueAnchor => ScreenAnchor;

        public void BeginCast()
        {
            EnemyTilemap.SetTile(ToTilePosition(Entity.Anchor), Entity.Definition.CastTile);
        }

        public void EndCast()
        {
            Vector3Int position = ToTilePosition(Entity.Anchor);
            if (EnemyTilemap.GetTile(position) == Entity.Definition.CastTile)
            {
                EnemyTilemap.SetTile(position, Entity.Definition.IdleTile);
            }
        }

        private static Vector3Int ToTilePosition(Vector2Int position)
        {
            return new Vector3Int(position.x, position.y);
        }
    }

    public class BossEntity
    {
        private GameplayAbilityRuntime abilityRuntime;
        private readonly GameplayAbilitySystem abilitySystem;

        public BossEntity(BossDefinition definition, Vector2Int anchor, int health, GameplayAbilitySystem abilitySystem)
        {
            Definition = definition;
            Anchor = anchor;
            Health = health;
            this.abilitySystem = abilitySystem;
        }

        public BossDefinition Definition { get; }
        public Vector2Int Anchor { get; }
        public int Health { get; private set; }
        public int HitCount { get; private set; }

        internal void InitializeAbility(GameplayAbilityRuntime runtime)
        {
            abilityRuntime = runtime;
        }

        public BossAttackResult ReceiveAttack(int attackPower)
        {
            HitCount++;
            int appliedAttackPower = Definition.GetIncomingDamage(attackPower, HitCount);
            int appliedDamage = Mathf.Min(appliedAttackPower, Health);
            Health -= appliedAttackPower;

            if (Health <= 0)
            {
                abilitySystem.CancelAll();
                return new BossAttackResult(appliedDamage, Health, 0, default);
            }

            int counterDamage = Definition.CounterDamage;
            Color flashColor = default;
            if (HitCount % Definition.AbilityHitInterval == 0)
            {
                if (abilityRuntime.TryActivate(Mathf.CeilToInt(Definition.CounterDamage * Definition.AbilityDamageMultiplier)))
                {
                    counterDamage = 0;
                    flashColor = Definition.AbilityFlashColor;
                }
            }
            else if (Definition.TryGetGuardedFlashColor(HitCount, out Color guardedColor))
            {
                flashColor = guardedColor;
            }

            return new BossAttackResult(appliedDamage, Health, counterDamage, flashColor);
        }

        public void CancelAbility()
        {
            abilitySystem.CancelAll();
        }
    }
}
