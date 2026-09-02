using UnityEngine;
using UnityEngine.Tilemaps;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Bosses/Boss", fileName = "Boss")]
    public sealed class BossDefinition : ScriptableObject
    {
        [SerializeField] private TileBase idleTile;
        [SerializeField] private TileBase castTile;
        [SerializeField, Min(1)] private int baseHealth = 1000;
        [SerializeField, Min(0)] private int counterDamage = 100;
        [SerializeField, Min(1)] private int abilityHitInterval = 3;
        [SerializeField, Min(1f)] private float abilityDamageMultiplier = 2f;
        [SerializeField] private Color abilityFlashColor = Color.white;
        [SerializeField, Min(1)] private int guardedHitInterval = 2;
        [SerializeField, Min(0)] private int guardedHitOffset = 1;
        [SerializeField, Range(0f, 1f)] private float guardedDamageReduction;
        [SerializeField] private Color guardedFlashColor = Color.white;
        [SerializeField] private BossAbilityDefinition ability;

        public TileBase IdleTile => idleTile;
        public TileBase CastTile => castTile;
        public int BaseHealth => baseHealth;
        public int CounterDamage => counterDamage;
        public int AbilityHitInterval => abilityHitInterval;
        public float AbilityDamageMultiplier => abilityDamageMultiplier;
        public Color AbilityFlashColor => abilityFlashColor;
        public BossAbilityDefinition Ability => ability;

        public int GetIncomingDamage(int attackPower, int hitCount)
        {
            if (!IsGuardedHit(hitCount))
            {
                return attackPower;
            }

            return Mathf.Max(1, Mathf.CeilToInt(attackPower * (1f - guardedDamageReduction)));
        }

        public bool TryGetGuardedFlashColor(int hitCount, out Color color)
        {
            color = guardedFlashColor;
            return IsGuardedHit(hitCount);
        }

        private bool IsGuardedHit(int hitCount)
        {
            return guardedDamageReduction > 0f
                && hitCount % guardedHitInterval == guardedHitOffset;
        }
    }
}
