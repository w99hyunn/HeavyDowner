using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class DestroyedCellVisual : MonoBehaviour
    {
        private static readonly int SHARD_INDEX_ID = Shader.PropertyToID("_ShardIndex");
        private static readonly int SPRITE_UV_RECT_ID = Shader.PropertyToID("_SpriteUVRect");

        [SerializeField] private Transform[] shardPivots;
        [SerializeField] private SpriteRenderer[] shardRenderers;
        [SerializeField] private Vector2[] shardOrigins;
        [SerializeField] private Vector2[] shardVelocityFactors =
        {
            new(-1.35f, 0.7f),
            new(-0.8f, 1.05f),
            new(-0.35f, 1.25f),
            new(0.1f, 0.9f),
            new(0.45f, 0.6f),
            new(0.75f, 1.15f),
            new(1.05f, 0.8f),
            new(1.35f, 0.55f)
        };
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

        public void Play(Sprite sprite, Vector3 position, Vector3 scale, float horizontalDirection)
        {
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one;

            Rect textureRect = sprite.textureRect;
            Texture texture = sprite.texture;
            Vector2 scaledSpriteSize = Vector2.Scale(sprite.bounds.size, scale);

            propertyBlock.Clear();
            propertyBlock.SetVector(SPRITE_UV_RECT_ID, new Vector4(textureRect.x / texture.width, textureRect.y / texture.height, textureRect.width / texture.width, textureRect.height / texture.height));

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

                Vector2 velocityFactor = shardVelocityFactors[index];
                velocities[index] = new Vector2(horizontalSpeed * (velocityFactor.x + horizontalDirection * 0.2f), jumpSpeed * velocityFactor.y);
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
                shardPivots[index].Rotate(0f, 0f, rotationSpeed * ((index & 1) == 0 ? -1f : 1f) * deltaTime, Space.Self);
                hasVisibleShard |= shardPivots[index].position.y >= despawnHeight;
            }

            if (!hasVisibleShard)
            {
                IsPlaying = false;
                gameObject.SetActive(false);
            }
        }

    }
}
