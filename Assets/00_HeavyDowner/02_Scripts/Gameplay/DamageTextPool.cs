using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace HeavyDowner.Gameplay
{
    public class DamageTextPool : MonoBehaviour
    {
        [SerializeField] private DamageTextVisual damageTextPrefab;
        [SerializeField] private Camera streamingCamera;
        [SerializeField] private Vector3 worldDamageOffset = new(0f, 0.2f, 0f);
        [SerializeField] private Vector3 playerDamageOffset = new(0f, 0.75f, 0f);

        private readonly List<DamageTextVisual> activeVisuals = new();
        private ObjectPool<DamageTextVisual> visualPool;

        private void Awake()
        {
            int initialCapacity = 12;
            visualPool = new ObjectPool<DamageTextVisual>(
                () => Instantiate(damageTextPrefab, transform),
                null,
                visual => visual.Initialize(),
                visual => Destroy(visual.gameObject),
                true,
                initialCapacity);

            DamageTextVisual[] prewarmedVisuals = new DamageTextVisual[initialCapacity];
            for (int index = 0; index < prewarmedVisuals.Length; index++)
            {
                prewarmedVisuals[index] = visualPool.Get();
            }
            for (int index = 0; index < prewarmedVisuals.Length; index++)
            {
                visualPool.Release(prewarmedVisuals[index]);
            }

            enabled = false;
        }

        private void Update()
        {
            float despawnHeight = streamingCamera.transform.position.y
                - streamingCamera.orthographicSize
                - 0.45f;

            for (int index = activeVisuals.Count - 1; index >= 0; index--)
            {
                DamageTextVisual visual = activeVisuals[index];
                visual.Tick(Time.deltaTime, despawnHeight);
                if (visual.IsPlaying)
                {
                    continue;
                }

                activeVisuals.RemoveAt(index);
                visualPool.Release(visual);
            }

            enabled = activeVisuals.Count > 0;
        }

        public void PlayWorldDamage(int damage, Vector3 worldPosition)
        {
            DamageTextVisual visual = visualPool.Get();
            activeVisuals.Add(visual);
            visual.PlayWorldDamage(damage, worldPosition + worldDamageOffset);
            enabled = true;
        }

        public void PlayPlayerDamage(int damage, Vector3 worldPosition)
        {
            DamageTextVisual visual = visualPool.Get();
            activeVisuals.Add(visual);
            visual.PlayPlayerDamage(damage, worldPosition + playerDamageOffset);
            enabled = true;
        }

        public void HideAll()
        {
            for (int index = activeVisuals.Count - 1; index >= 0; index--)
            {
                visualPool.Release(activeVisuals[index]);
            }

            activeVisuals.Clear();
            enabled = false;
        }

        private void OnDestroy()
        {
            HideAll();
            visualPool.Clear();
        }
    }
}
