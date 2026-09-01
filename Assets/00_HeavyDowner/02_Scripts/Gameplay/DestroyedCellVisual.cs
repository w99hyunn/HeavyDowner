using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public sealed class DestroyedCellVisual : MonoBehaviour
    {
        private static readonly int SHARD_INDEX_ID = Shader.PropertyToID("_ShardIndex");
        private static readonly int SPRITE_UV_RECT_ID = Shader.PropertyToID("_SpriteUVRect");

        [SerializeField] private Transform[] shardPivots;
        [SerializeField] private SpriteRenderer[] shardRenderers;
        [SerializeField] private Vector2[] shardOrigins;
        [SerializeField] private float shardScale = 1.3f;
        [SerializeField] private float jumpSpeed = 0.75f;
        [SerializeField] private float horizontalSpeed = 0.45f;
        [SerializeField] private float gravity = 5.5f;
        [SerializeField] private float rotationSpeed = 320f;

        private Vector2[] velocities;
        private MaterialPropertyBlock propertyBlock;

        public bool IsPlaying { get; private set; }

        public void Initialize()
        {
            velocities = new Vector2[shardPivots.Length];
            propertyBlock = new MaterialPropertyBlock();
            IsPlaying = false;
            gameObject.SetActive(false);
        }

        public void Play(
            Sprite sprite,
            Vector3 position,
            Vector3 scale,
            float horizontalDirection)
        {
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one;

            Rect textureRect = sprite.textureRect;
            Texture texture = sprite.texture;
            Vector4 spriteUvRect = new Vector4(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            Vector2 scaledSpriteSize = Vector2.Scale(sprite.bounds.size, scale);

            propertyBlock.Clear();
            propertyBlock.SetVector(SPRITE_UV_RECT_ID, spriteUvRect);

            for (int index = 0; index < shardPivots.Length; index++)
            {
                Vector3 origin = Vector2.Scale(shardOrigins[index], scaledSpriteSize);
                Transform pivot = shardPivots[index];
                SpriteRenderer shardRenderer = shardRenderers[index];

                pivot.localPosition = origin;
                pivot.localRotation = Quaternion.identity;
                shardRenderer.transform.localPosition = -origin;
                shardRenderer.transform.localScale = scale * shardScale;
                shardRenderer.sprite = sprite;
                shardRenderer.color = Color.white;

                propertyBlock.SetFloat(SHARD_INDEX_ID, index);
                shardRenderer.SetPropertyBlock(propertyBlock);

                velocities[index] = new Vector2(
                    horizontalSpeed * (GetHorizontalSpread(index) + horizontalDirection * 0.2f),
                    jumpSpeed * GetVerticalSpread(index));
            }

            IsPlaying = true;
            gameObject.SetActive(true);
        }

        public void Tick(float deltaTime, float despawnHeight)
        {
            bool hasVisibleShard = false;

            for (int index = 0; index < shardPivots.Length; index++)
            {
                velocities[index].y -= gravity * deltaTime;
                shardPivots[index].localPosition += (Vector3)(velocities[index] * deltaTime);
                float rotationDirection = (index & 1) == 0 ? -1f : 1f;
                shardPivots[index].Rotate(
                    0f,
                    0f,
                    rotationSpeed * rotationDirection * deltaTime,
                    Space.Self);
                hasVisibleShard |= shardPivots[index].position.y >= despawnHeight;
            }

            if (!hasVisibleShard)
            {
                IsPlaying = false;
                gameObject.SetActive(false);
            }
        }

        private static float GetHorizontalSpread(int index)
        {
            return index switch
            {
                0 => -1.35f,
                1 => -0.8f,
                2 => -0.35f,
                3 => 0.1f,
                4 => 0.45f,
                5 => 0.75f,
                6 => 1.05f,
                _ => 1.35f
            };
        }

        private static float GetVerticalSpread(int index)
        {
            return index switch
            {
                0 => 0.7f,
                1 => 1.05f,
                2 => 1.25f,
                3 => 0.9f,
                4 => 0.6f,
                5 => 1.15f,
                6 => 0.8f,
                _ => 0.55f
            };
        }
    }
}
