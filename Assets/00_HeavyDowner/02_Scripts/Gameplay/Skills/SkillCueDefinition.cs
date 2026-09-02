using UnityEngine;

namespace HeavyDowner.Gameplay
{
    [CreateAssetMenu(menuName = "Heavy Downer/Skills/Cue", fileName = "SkillCue")]
    public class SkillCueDefinition : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private AudioClip[] audioClips = new AudioClip[0];
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField, Min(1)] private int poolSize = 1;
        [SerializeField, Min(0f)] private float lifetime = 2f;
        [SerializeField] private float rotationSpeed;

        public GameObject Prefab => prefab;
        public bool HasAudio => audioClips.Length > 0;
        public int PoolSize => poolSize;
        public float Lifetime => lifetime;
        public float RotationSpeed => rotationSpeed;

        public AudioClip GetRandomAudioClip()
        {
            return audioClips[Random.Range(0, audioClips.Length)];
        }

        public float GetRandomPitch()
        {
            return Random.Range(Mathf.Min(pitchRange.x, pitchRange.y), Mathf.Max(pitchRange.x, pitchRange.y));
        }
    }
}
