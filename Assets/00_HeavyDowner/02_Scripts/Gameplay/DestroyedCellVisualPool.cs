using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class DestroyedCellVisualPool : MonoBehaviour
    {
        [SerializeField] private Camera streamingCamera;

        private DestroyedCellVisual[] visuals;
        private int nextVisualIndex;

        private void Awake()
        {
            visuals = GetComponentsInChildren<DestroyedCellVisual>(true);
            for (int index = 0; index < visuals.Length; index++)
            {
                visuals[index].Initialize();
            }

            enabled = false;
        }

        private void Update()
        {
            float despawnHeight = streamingCamera.transform.position.y
                - streamingCamera.orthographicSize
                - 0.45f;

            bool hasPlayingVisual = false;
            for (int index = 0; index < visuals.Length; index++)
            {
                if (visuals[index].IsPlaying)
                {
                    visuals[index].Tick(Time.deltaTime, despawnHeight);
                    hasPlayingVisual |= visuals[index].IsPlaying;
                }
            }

            enabled = hasPlayingVisual;
        }

        public void Play(Sprite sprite, Vector3 position, Vector3 scale)
        {
            GetAvailableVisual().Play(sprite, position, scale, Mathf.Sign(position.x - transform.position.x));
            enabled = true;
        }

        private DestroyedCellVisual GetAvailableVisual()
        {
            for (int offset = 0; offset < visuals.Length; offset++)
            {
                int index = (nextVisualIndex + offset) % visuals.Length;
                if (!visuals[index].IsPlaying)
                {
                    nextVisualIndex = (index + 1) % visuals.Length;
                    return visuals[index];
                }
            }

            DestroyedCellVisual visual = visuals[nextVisualIndex];
            nextVisualIndex = (nextVisualIndex + 1) % visuals.Length;
            return visual;
        }
    }
}
