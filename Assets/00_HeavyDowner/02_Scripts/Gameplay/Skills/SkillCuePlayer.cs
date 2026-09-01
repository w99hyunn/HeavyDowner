using System.Collections.Generic;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public sealed class SkillCuePlayer : MonoBehaviour
    {
        private sealed class CueObject
        {
            public GameObject Root;
            public ParticleSystem[] Particles;
            public AudioSource AudioSource;
            public Quaternion InitialRotation;
        }

        private sealed class CuePool
        {
            public SkillCueDefinition Definition;
            public readonly Queue<CueObject> Available = new();
            public readonly List<CueObject> All = new();
        }

        private sealed class ActiveCue
        {
            public int Id;
            public CuePool Pool;
            public CueObject CueObject;
            public Transform Anchor;
            public float EndTime;
        }

        private readonly Dictionary<SkillCueDefinition, CuePool> pools = new();
        private readonly List<ActiveCue> activeCues = new();
        private int nextHandleId = 1;

        private void Update()
        {
            for (int i = activeCues.Count - 1; i >= 0; i--)
            {
                ActiveCue activeCue = activeCues[i];
                if (activeCue.Anchor != null)
                {
                    activeCue.CueObject.Root.transform.position = activeCue.Anchor.position;
                }

                activeCue.CueObject.Root.transform.Rotate(
                    0f,
                    0f,
                    activeCue.Pool.Definition.RotationSpeed * Time.deltaTime,
                    Space.Self);

                if (activeCue.Anchor == null && Time.time >= activeCue.EndTime)
                {
                    Release(i);
                }
            }
        }

        private void OnDisable()
        {
            for (int i = activeCues.Count - 1; i >= 0; i--)
            {
                Release(i);
            }
        }

        private void OnDestroy()
        {
            foreach (CuePool pool in pools.Values)
            {
                foreach (CueObject cueObject in pool.All)
                {
                    Destroy(cueObject.Root);
                }
            }
        }

        public void Prewarm(SkillCueDefinition definition)
        {
            if (pools.ContainsKey(definition))
            {
                return;
            }

            CuePool pool = new()
            {
                Definition = definition
            };
            pools.Add(definition, pool);

            for (int i = 0; i < definition.PoolSize; i++)
            {
                CueObject cueObject = CreateCueObject(definition);
                pool.All.Add(cueObject);
                pool.Available.Enqueue(cueObject);
            }
        }

        public void PlayOneShot(SkillCueDefinition definition, Vector3 position)
        {
            CuePool pool = pools[definition];
            CueObject cueObject = Rent(pool, position);
            float lifetime = definition.Lifetime;
            if (definition.HasAudio)
            {
                float audioLifetime = cueObject.AudioSource.clip.length
                    / Mathf.Max(0.01f, Mathf.Abs(cueObject.AudioSource.pitch));
                lifetime = Mathf.Max(lifetime, audioLifetime);
            }

            activeCues.Add(new ActiveCue
            {
                Pool = pool,
                CueObject = cueObject,
                EndTime = Time.time + lifetime
            });
        }

        public SkillCueHandle PlayLoop(SkillCueDefinition definition, Transform anchor)
        {
            CuePool pool = pools[definition];
            CueObject cueObject = Rent(pool, anchor.position);
            int handleId = nextHandleId++;
            activeCues.Add(new ActiveCue
            {
                Id = handleId,
                Pool = pool,
                CueObject = cueObject,
                Anchor = anchor
            });
            return new SkillCueHandle(handleId);
        }

        public void Stop(SkillCueHandle handle)
        {
            for (int i = activeCues.Count - 1; i >= 0; i--)
            {
                if (activeCues[i].Id == handle.Id)
                {
                    Release(i);
                    return;
                }
            }
        }

        private CueObject Rent(CuePool pool, Vector3 position)
        {
            if (pool.Available.Count == 0)
            {
                CueObject expandedCueObject = CreateCueObject(pool.Definition);
                pool.All.Add(expandedCueObject);
                pool.Available.Enqueue(expandedCueObject);
            }

            CueObject cueObject = pool.Available.Dequeue();
            Transform cueTransform = cueObject.Root.transform;
            cueTransform.position = position;
            cueTransform.localRotation = cueObject.InitialRotation;
            cueObject.Root.SetActive(true);

            foreach (ParticleSystem particle in cueObject.Particles)
            {
                particle.Clear(false);
                particle.Play(false);
            }

            if (pool.Definition.HasAudio)
            {
                cueObject.AudioSource.clip = pool.Definition.GetRandomAudioClip();
                cueObject.AudioSource.pitch = pool.Definition.GetRandomPitch();
                cueObject.AudioSource.Play();
            }

            return cueObject;
        }

        private void Release(int activeIndex)
        {
            ActiveCue activeCue = activeCues[activeIndex];
            activeCue.CueObject.AudioSource.Stop();
            activeCue.CueObject.AudioSource.clip = null;

            activeCue.CueObject.Root.SetActive(false);
            activeCue.Pool.Available.Enqueue(activeCue.CueObject);
            activeCues.RemoveAt(activeIndex);
        }

        private CueObject CreateCueObject(SkillCueDefinition definition)
        {
            GameObject root = Instantiate(definition.Prefab, transform.parent);

            ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
            root.TryGetComponent<AudioSource>(out AudioSource audioSource);

            root.SetActive(false);
            return new CueObject
            {
                Root = root,
                Particles = particles,
                AudioSource = audioSource,
                InitialRotation = root.transform.localRotation
            };
        }
    }
}
