using System;
using HeavyDowner.UI;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class IngamePlayerController : MonoBehaviour
    {
        private static readonly int FALL_STATE_HASH = Animator.StringToHash("FallState");

        [SerializeField] private float repeatDelay = 0.18f;
        [SerializeField] private VerticalTilemapWorld world;
        [SerializeField] private DescentBoardController board;
        [SerializeField] private JoystickControl joystick;
        [SerializeField] private int attackPower = 1;
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private float damageFlashDuration = 0.12f;
        [SerializeField] private Color damageColor = new(1f, 0.52f, 0.52f, 1f);

        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private Vector2Int activeStepDirection;
        private float nextStepTime;
        private float damageFlashEndTime;
        private int currentHealth;
        private int currentDepth;
        private FallAnimation currentAnimation;
        private bool isDead;

        public event Action<float> HealthChanged;
        public event Action<int> DepthChanged;
        public event Action Died;

        public float HealthNormalized => (float)currentHealth / maxHealth;
        public int CurrentDepth => currentDepth;

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
            currentHealth = maxHealth;
        }

        private void Start()
        {
            HealthChanged?.Invoke(HealthNormalized);
            DepthChanged?.Invoke(currentDepth);
        }

        private void Update()
        {
            if (isDead)
            {
                RestoreDamageColor();
                return;
            }

            Vector2 direction = joystick.Direction;
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
            BoardActionResult action = board.Attack(targetCell, attackPower);
            if (action.CounterDamage > 0)
            {
                TakeDamage(action.CounterDamage);
                if (isDead)
                {
                    return;
                }
            }

            if (!action.CanEnter)
            {
                return;
            }

            transform.position = world.CellToWorld(targetCell);

            int depth = Mathf.Max(0, -targetCell.y);
            if (depth != currentDepth)
            {
                currentDepth = depth;
                DepthChanged?.Invoke(currentDepth);
            }
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
            currentHealth = Mathf.Max(0, currentHealth - damage);
            HealthChanged?.Invoke(HealthNormalized);
            spriteRenderer.color = damageColor;
            damageFlashEndTime = Time.time + damageFlashDuration;

            if (currentHealth > 0)
            {
                return;
            }

            isDead = true;
            Died?.Invoke();
        }

        private void RestoreDamageColor()
        {
            if (damageFlashEndTime <= 0f || Time.time < damageFlashEndTime)
            {
                return;
            }

            damageFlashEndTime = 0f;
            spriteRenderer.color = Color.white;
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
    }
}
