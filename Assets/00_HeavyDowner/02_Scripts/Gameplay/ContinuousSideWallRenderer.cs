using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [DefaultExecutionOrder(100)]
    public class ContinuousSideWallRenderer : MonoBehaviour
    {
        [SerializeField] private Transform streamingCamera;
        [SerializeField] private SpriteRenderer[] leftSegments;
        [SerializeField] private SpriteRenderer[] rightSegments;

        private float segmentHeight;
        private int currentCenterSegment = int.MaxValue;

        private void Awake()
        {
            segmentHeight = leftSegments[0].sprite.bounds.size.y
                * Mathf.Abs(leftSegments[0].transform.localScale.y);
            RefreshSegments();
        }

        private void LateUpdate()
        {
            RefreshSegments();
        }

        private void RefreshSegments()
        {
            int centerSegment = Mathf.RoundToInt(streamingCamera.position.y / segmentHeight);
            if (centerSegment == currentCenterSegment)
            {
                return;
            }

            currentCenterSegment = centerSegment;
            int firstSegment = centerSegment - leftSegments.Length / 2;
            for (int index = 0; index < leftSegments.Length; index++)
            {
                int segment = firstSegment + index;
                PositionSegment(leftSegments[index], segment, false);
                PositionSegment(rightSegments[index], segment, true);
            }
        }

        private void PositionSegment(SpriteRenderer renderer, int segment, bool mirrorHorizontally)
        {
            Transform segmentTransform = renderer.transform;
            Vector3 position = segmentTransform.position;
            position.y = segment * segmentHeight;
            segmentTransform.position = position;

            Vector3 scale = segmentTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * (mirrorHorizontally ? -1f : 1f);
            scale.y = Mathf.Abs(scale.y) * (segment % 2 == 0 ? 1f : -1f);
            segmentTransform.localScale = scale;
        }
    }
}
