using TMPro;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public sealed class DamageTextVisual : MonoBehaviour
    {
        [SerializeField] private TMP_Text damageText;

        [Header("Block / Enemy")]
        [SerializeField] private Color worldDamageColor = new(1f, 0.72f, 0.15f, 1f);
        [SerializeField] private float jumpSpeed = 0.75f;
        [SerializeField] private float horizontalSpeed = 0.45f;
        [SerializeField] private float gravity = 5.5f;

        [Header("Player")]
        [SerializeField] private Color playerDamageColor = new(1f, 0.32f, 0.25f, 1f);
        [SerializeField] private float playerHoldDuration = 0.12f;
        [SerializeField] private float playerFadeDuration = 0.45f;

        private Vector2 velocity;
        private Color activeColor;
        private float elapsedTime;
        private bool isBallistic;

        public bool IsPlaying { get; private set; }

        public void Initialize()
        {
            IsPlaying = false;
            gameObject.SetActive(false);
        }

        public void PlayWorldDamage(int damage, Vector3 position)
        {
            Play(position, worldDamageColor);
            damageText.SetText("{0}", damage);
            velocity = new Vector2(
                Random.Range(-horizontalSpeed, horizontalSpeed),
                jumpSpeed);
            isBallistic = true;
        }

        public void PlayPlayerDamage(int damage, Vector3 position)
        {
            Play(position, playerDamageColor);
            damageText.SetText("-{0}", damage);
            velocity = Vector2.zero;
            isBallistic = false;
        }

        public void Tick(float deltaTime, float despawnHeight)
        {
            elapsedTime += deltaTime;

            if (isBallistic)
            {
                velocity.y -= gravity * deltaTime;
                transform.position += (Vector3)(velocity * deltaTime);
                if (transform.position.y < despawnHeight)
                {
                    Stop();
                }

                return;
            }

            float fadeProgress = Mathf.InverseLerp(
                playerHoldDuration,
                playerHoldDuration + playerFadeDuration,
                elapsedTime);
            Color color = activeColor;
            color.a = 1f - fadeProgress;
            damageText.color = color;

            if (fadeProgress >= 1f)
            {
                Stop();
            }
        }

        private void Play(Vector3 position, Color color)
        {
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one;
            damageText.color = color;
            activeColor = color;
            elapsedTime = 0f;
            IsPlaying = true;
            gameObject.SetActive(true);
        }

        private void Stop()
        {
            IsPlaying = false;
            gameObject.SetActive(false);
        }
    }
}
