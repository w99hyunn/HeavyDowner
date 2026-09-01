using System.Collections.Generic;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public sealed class DamageTextPool : MonoBehaviour
    {
        [SerializeField] private DamageTextVisual damageTextPrefab;
        [SerializeField] private Camera streamingCamera;
        [SerializeField, Min(1)] private int initialCapacity = 12;
        [SerializeField] private Vector3 worldDamageOffset = new(0f, 0.2f, 0f);
        [SerializeField] private Vector3 playerDamageOffset = new(0f, 0.75f, 0f);
        [SerializeField] private float despawnPadding = 0.45f;

        private readonly List<DamageTextVisual> visuals = new();

        private void Awake()
        {
            for (int index = 0; index < initialCapacity; index++)
            {
                CreateVisual();
            }

            enabled = false;
        }

        private void Update()
        {
            float despawnHeight = streamingCamera.transform.position.y
                - streamingCamera.orthographicSize
                - despawnPadding;

            bool hasPlayingVisual = false;
            for (int index = 0; index < visuals.Count; index++)
            {
                DamageTextVisual visual = visuals[index];
                if (!visual.IsPlaying)
                {
                    continue;
                }

                visual.Tick(Time.deltaTime, despawnHeight);
                hasPlayingVisual |= visual.IsPlaying;
            }

            enabled = hasPlayingVisual;
        }

        public void PlayWorldDamage(int damage, Vector3 worldPosition)
        {
            GetAvailableVisual().PlayWorldDamage(damage, worldPosition + worldDamageOffset);
            enabled = true;
        }

        public void PlayPlayerDamage(int damage, Vector3 worldPosition)
        {
            GetAvailableVisual().PlayPlayerDamage(damage, worldPosition + playerDamageOffset);
            enabled = true;
        }

        public void HideAll()
        {
            for (int index = 0; index < visuals.Count; index++)
            {
                visuals[index].Initialize();
            }

            enabled = false;
        }

        private DamageTextVisual GetAvailableVisual()
        {
            for (int index = 0; index < visuals.Count; index++)
            {
                if (!visuals[index].IsPlaying)
                {
                    return visuals[index];
                }
            }

            return CreateVisual();
        }

        private DamageTextVisual CreateVisual()
        {
            DamageTextVisual visual = Instantiate(damageTextPrefab, transform);
            visual.Initialize();
            visuals.Add(visual);
            return visual;
        }
    }
}
