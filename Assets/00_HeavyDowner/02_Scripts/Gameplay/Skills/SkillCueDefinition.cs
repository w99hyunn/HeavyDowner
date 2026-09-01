using UnityEngine;
using UnityEngine.Audio;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Skills/Cue", fileName = "SkillCue")]
    public sealed class SkillCueDefinition : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private AudioClip[] audioClips = new AudioClip[0];
        [SerializeField] private AudioMixerGroup audioOutput;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField, Range(0f, 1f)] private float spatialBlend;
        [SerializeField] private Vector3 scale = Vector3.one;
        [SerializeField] private Vector3 rotation;
        [SerializeField] private bool overrideColor;
        [SerializeField] private Color color = Color.white;
        [SerializeField, Min(1)] private int poolSize = 1;
        [SerializeField, Min(0f)] private float lifetime = 2f;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private int sortingOrder = 11;

        public GameObject Prefab => prefab;
        public bool HasAudio => audioClips.Length > 0;
        public AudioMixerGroup AudioOutput => audioOutput;
        public float Volume => volume;
        public float SpatialBlend => spatialBlend;
        public Vector3 Scale => scale;
        public Vector3 Rotation => rotation;
        public bool OverrideColor => overrideColor;
        public Color Color => color;
        public int PoolSize => poolSize;
        public float Lifetime => lifetime;
        public float RotationSpeed => rotationSpeed;
        public int SortingOrder => sortingOrder;

        public AudioClip GetRandomAudioClip()
        {
            return audioClips[Random.Range(0, audioClips.Length)];
        }

        public float GetRandomPitch()
        {
            float minimumPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maximumPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            return Random.Range(minimumPitch, maximumPitch);
        }
    }
}
