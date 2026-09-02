using System;
using HeavyDowner.Module;
using HeavyDowner.UI;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class IngamePlayerController : MonoBehaviour, ISkillActor
    {
        private static readonly int FALL_STATE_HASH = Animator.StringToHash("FallState");

        [SerializeField] private float repeatDelay = 0.18f;
        [SerializeField] private VerticalTilemapWorld world;
        [SerializeField] private DescentBoardController board;
        [SerializeField] private JoystickControl joystick;
        [SerializeField] private DamageTextPool damageTextPool;
        [SerializeField] private EquipmentCatalog equipmentCatalog;
        [SerializeField] private int attackPower = 100;
        [SerializeField] private int maxHealth = 10000;
        [SerializeField] private float damageFlashDuration = 0.12f;
        [SerializeField] private Color damageColor = new(1f, 0.52f, 0.52f, 1f);

        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private Vector2Int activeStepDirection;
        private float nextStepTime;
        private float damageFlashEndTime;
        private int currentHealth;
        private int currentMaxHealth;
        private int currentAttackPower;
        private int currentAttackRadius;
        private int currentShield;
        private int currentDepth;
        private FallAnimation currentAnimation;
        private bool isMovementLocked;
        private float damageReduction;
        private float damageRemainder;

        public event Action<float> HealthChanged;
        public event Action<float> ShieldChanged;
        public event Action ShieldDepleted;
        public event Action<int> DepthChanged;
        public event Action Died;

        public float HealthNormalized => (float)currentHealth / currentMaxHealth;
        public float ShieldNormalized => (float)currentShield / currentMaxHealth;
        public int CurrentDepth => currentDepth;
        public bool IsDead => currentHealth <= 0;
        public Vector2Int CurrentCell => world.WorldToCell(transform.position);
        public Transform SkillTransform => transform;

        private enum FallAnimation
        {
            Front,
            Left,
            Right,
            Dive
        }

        private void Awake()
        {
            TryGetComponent<Animator>(out animator);
            TryGetComponent<SpriteRenderer>(out spriteRenderer);
            ApplyEquipmentStats();
            currentHealth = currentMaxHealth;
        }

        private void Start()
        {
            HealthChanged?.Invoke(HealthNormalized);
            ShieldChanged?.Invoke(ShieldNormalized);
            DepthChanged?.Invoke(currentDepth);
        }

        private void Update()
        {
            if (IsDead)
            {
                RestoreDamageColor();
                return;
            }

            Vector2 direction = isMovementLocked ? Vector2.zero : joystick.Direction;
            Move(direction);
            Animate(direction);
            RestoreDamageColor();
        }

        private void Move(Vector2 direction)
        {
            Vector2Int stepDirection = ResolveStepDirection(direction);
            if (stepDirection == Vector2Int.zero)
            {
                activeStepDirection = Vector2Int.zero;
                return;
            }

            if (stepDirection == activeStepDirection && Time.time < nextStepTime)
            {
                return;
            }

            activeStepDirection = stepDirection;
            nextStepTime = Time.time + repeatDelay;

            Vector2Int currentCell = world.WorldToCell(transform.position);
            Vector2Int targetCell = currentCell + stepDirection;
            BoardActionResult action = board.Attack(targetCell, currentAttackPower);
            if (currentAttackRadius > 0)
            {
                board.AttackSplash(targetCell, currentAttackRadius, Mathf.Max(1, currentAttackPower / 2));
            }
            if (action.CounterDamage > 0)
            {
                TakeDamage(action.CounterDamage);
                if (IsDead)
                {
                    return;
                }
            }

            if (!action.CanEnter)
            {
                return;
            }

            MoveToCell(targetCell);
        }

        private static Vector2Int ResolveStepDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.16f)
            {
                return Vector2Int.zero;
            }

            if (direction.y < -0.4f)
            {
                if (direction.x < -0.4f)
                {
                    return Vector2Int.down + Vector2Int.left;
                }

                if (direction.x > 0.4f)
                {
                    return Vector2Int.down + Vector2Int.right;
                }

                return Vector2Int.down;
            }

            if (direction.x < -0.4f)
            {
                return Vector2Int.left;
            }

            if (direction.x > 0.4f)
            {
                return Vector2Int.right;
            }

            return Vector2Int.zero;
        }

        private void Animate(Vector2 direction)
        {
            FallAnimation nextAnimation = ResolveAnimation(direction);
            if (nextAnimation != currentAnimation)
            {
                currentAnimation = nextAnimation;
                animator.SetInteger(FALL_STATE_HASH, (int)currentAnimation);
            }
        }

        private void TakeDamage(int damage)
        {
            float reducedDamageValue = damage * (1f - damageReduction) + damageRemainder;
            int reducedDamage = Mathf.FloorToInt(reducedDamageValue);
            damageRemainder = reducedDamageValue - reducedDamage;
            if (reducedDamage == 0)
            {
                return;
            }

            int remainingDamage = reducedDamage;
            if (currentShield > 0)
            {
                int absorbedDamage = Mathf.Min(currentShield, remainingDamage);
                currentShield -= absorbedDamage;
                remainingDamage -= absorbedDamage;
                ShieldChanged?.Invoke(ShieldNormalized);

                if (currentShield == 0)
                {
                    damageReduction = 0f;
                    damageRemainder = 0f;
                    ShieldDepleted?.Invoke();
                }
            }

            if (remainingDamage > 0)
            {
                currentHealth = Mathf.Max(0, currentHealth - remainingDamage);
                HealthChanged?.Invoke(HealthNormalized);
                damageTextPool.PlayPlayerDamage(remainingDamage, transform.position);
            }

            spriteRenderer.color = damageColor;
            damageFlashEndTime = Time.time + damageFlashDuration;

            if (currentHealth > 0)
            {
                return;
            }

            Died?.Invoke();
        }

        public void SetMovementLocked(bool locked)
        {
            isMovementLocked = locked;
            activeStepDirection = Vector2Int.zero;

            if (!locked)
            {
                return;
            }

            currentAnimation = FallAnimation.Dive;
            animator.SetInteger(FALL_STATE_HASH, (int)currentAnimation);
        }

        public void MoveToCell(Vector2Int targetCell)
        {
            transform.position = world.CellToWorld(targetCell);

            int depth = Mathf.Max(0, -targetCell.y);
            if (depth == currentDepth)
            {
                return;
            }

            currentDepth = depth;
            DepthChanged?.Invoke(currentDepth);
        }

        public void ActivateShield(float reduction, float shieldHealthNormalized)
        {
            damageReduction = reduction;
            damageRemainder = 0f;
            currentShield = Mathf.Max(1, Mathf.CeilToInt(currentMaxHealth * shieldHealthNormalized));
            spriteRenderer.color = new Color(0.55f, 0.88f, 1f, 1f);
            ShieldChanged?.Invoke(ShieldNormalized);
        }

        public void DeactivateShield()
        {
            damageReduction = 0f;
            damageRemainder = 0f;
            currentShield = 0;
            spriteRenderer.color = Color.white;
            ShieldChanged?.Invoke(ShieldNormalized);
        }

        private void RestoreDamageColor()
        {
            if (damageFlashEndTime <= 0f || Time.time < damageFlashEndTime)
            {
                return;
            }

            damageFlashEndTime = 0f;
            spriteRenderer.color = currentShield > 0
                ? new Color(0.55f, 0.88f, 1f, 1f)
                : Color.white;
        }

        private static FallAnimation ResolveAnimation(Vector2 direction)
        {
            if (direction.y < -0.4f && Mathf.Abs(direction.x) < 0.35f)
            {
                return FallAnimation.Dive;
            }

            if (direction.x < -0.2f)
            {
                return FallAnimation.Left;
            }

            if (direction.x > 0.2f)
            {
                return FallAnimation.Right;
            }

            return FallAnimation.Front;
        }

        private void ApplyEquipmentStats()
        {
            currentAttackPower = attackPower;
            currentAttackRadius = 0;
            currentMaxHealth = maxHealth;

            if (PlayerDataService.EquippedWeapon != EquipmentId.None)
            {
                EquipmentDefinition weapon = equipmentCatalog.Get(PlayerDataService.EquippedWeapon);
                int level = PlayerDataService.GetEquipmentLevel(weapon.Id);
                currentAttackPower += weapon.GetAttackBonus(level);
                currentAttackRadius += weapon.GetAttackRadius(level);
            }

            if (PlayerDataService.EquippedArmor != EquipmentId.None)
            {
                EquipmentDefinition armor = equipmentCatalog.Get(PlayerDataService.EquippedArmor);
                int level = PlayerDataService.GetEquipmentLevel(armor.Id);
                currentMaxHealth += armor.GetHealthBonus(level);
            }
        }
    }
}
