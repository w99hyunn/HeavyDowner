using System.Collections.Generic;
using UnityEngine;

namespace HeavyDowner.Gameplay
{
    public class GameplayCuePlayer : MonoBehaviour
    {
        private class CueObject
        {
            public GameObject Root;
            public ParticleSystem[] Particles;
            public AudioSource AudioSource;
            public Quaternion InitialRotation;
            public Vector3 InitialScale;
        }

        private class CuePool
        {
            public GameplayCueDefinition Definition;
            public readonly Queue<CueObject> Available = new();
            public readonly List<CueObject> All = new();
        }

        private class ActiveCue
        {
            public int Id;
            public CuePool Pool;
            public CueObject CueObject;
            public Transform Anchor;
            public Vector3 AnchorOffset;
            public float EndTime;
            public bool IsLooping;
        }

        private readonly Dictionary<GameplayCueDefinition, CuePool> pools = new();
        private readonly List<ActiveCue> activeCues = new();
        private int nextHandleId = 1;

        private void Update()
        {
            for (int i = activeCues.Count - 1; i >= 0; i--)
            {
                ActiveCue activeCue = activeCues[i];
                if (activeCue.Anchor != null)
                {
                    activeCue.CueObject.Root.transform.position =
                        activeCue.Anchor.TransformPoint(activeCue.AnchorOffset);
                }

                activeCue.CueObject.Root.transform.Rotate(0f, 0f, activeCue.Pool.Definition.RotationSpeed * Time.deltaTime, Space.Self);

                if (!activeCue.IsLooping && Time.time >= activeCue.EndTime)
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

        public void Prewarm(GameplayCueDefinition definition)
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

        public GameplayCueHandle PlayOneShot(GameplayCueDefinition definition, Vector3 position)
        {
            CuePool pool = pools[definition];
            CueObject cueObject = Rent(pool, position);
            int handleId = nextHandleId++;

            activeCues.Add(new ActiveCue
            {
                Id = handleId,
                Pool = pool,
                CueObject = cueObject,
                EndTime = Time.time + GetLifetime(definition, cueObject)
            });
            return new GameplayCueHandle(handleId);
        }

        public GameplayCueHandle PlayOneShot(GameplayCueDefinition definition, Transform anchor, Vector3 localPosition)
        {
            CuePool pool = pools[definition];
            CueObject cueObject = Rent(pool, anchor.TransformPoint(localPosition));
            int handleId = nextHandleId++;
            activeCues.Add(new ActiveCue
            {
                Id = handleId,
                Pool = pool,
                CueObject = cueObject,
                Anchor = anchor,
                AnchorOffset = localPosition,
                EndTime = Time.time + GetLifetime(definition, cueObject)
            });
            return new GameplayCueHandle(handleId);
        }

        public GameplayCueHandle PlayLoop(GameplayCueDefinition definition, Transform anchor)
        {
            CuePool pool = pools[definition];
            int handleId = nextHandleId++;
            activeCues.Add(new ActiveCue
            {
                Id = handleId,
                Pool = pool,
                CueObject = Rent(pool, anchor.position),
                Anchor = anchor,
                IsLooping = true
            });
            return new GameplayCueHandle(handleId);
        }

        public void Stop(GameplayCueHandle handle)
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

        public void SetScale(GameplayCueHandle handle, float scale)
        {
            for (int index = activeCues.Count - 1; index >= 0; index--)
            {
                if (activeCues[index].Id == handle.Id)
                {
                    CueObject cueObject = activeCues[index].CueObject;
                    cueObject.Root.transform.localScale = cueObject.InitialScale * scale;
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
            cueTransform.localScale = cueObject.InitialScale;
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

        private CueObject CreateCueObject(GameplayCueDefinition definition)
        {
            GameObject root = definition.Prefab != null
                ? Instantiate(definition.Prefab, transform.parent)
                : new GameObject(definition.name);
            root.transform.SetParent(transform.parent, true);

            root.TryGetComponent<AudioSource>(out AudioSource audioSource);
            if (audioSource == null)
            {
                audioSource = root.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            root.SetActive(false);
            return new CueObject
            {
                Root = root,
                Particles = root.GetComponentsInChildren<ParticleSystem>(true),
                AudioSource = audioSource,
                InitialRotation = root.transform.localRotation,
                InitialScale = root.transform.localScale
            };
        }

        private static float GetLifetime(GameplayCueDefinition definition, CueObject cueObject)
        {
            float lifetime = definition.Lifetime;
            if (definition.HasAudio)
            {
                lifetime = Mathf.Max(lifetime, cueObject.AudioSource.clip.length / Mathf.Max(0.01f, Mathf.Abs(cueObject.AudioSource.pitch)));
            }

            return lifetime;
        }
    }
}
