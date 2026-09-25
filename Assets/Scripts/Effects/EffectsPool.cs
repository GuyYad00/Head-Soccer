using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Object pooling for the short lived effects. A goal spawns dozens of particles and
    /// a match can produce hundreds of kick sparks, so Instantiate and Destroy would
    /// create garbage collection spikes. A dropped frame during a kick is an unfair miss,
    /// so every effect instance is created once up front and recycled forever.
    /// </summary>
    public class EffectsPool : MonoBehaviour
    {
        public static EffectsPool Instance { get; private set; }

        [SerializeField] private GameObject kickSparkPrefab;
        [SerializeField] private GameObject goalConfettiPrefab;
        [SerializeField] private int poolSizePerEffect = 20;
        [SerializeField] private float effectLifetime = 1.5f;

        private Pool sparks;
        private Pool confetti;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            sparks = new Pool(kickSparkPrefab, poolSizePerEffect, transform);
            confetti = new Pool(goalConfettiPrefab, Mathf.Max(4, poolSizePerEffect / 4), transform);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SpawnKickSpark(Vector3 position) => Spawn(sparks, position);

        public void SpawnGoalConfetti(Vector3 position) => Spawn(confetti, position);

        private void Spawn(Pool pool, Vector3 position)
        {
            GameObject instance = pool?.Get(position);
            if (instance != null)
                StartCoroutine(ReturnAfterLifetime(pool, instance));
        }

        private IEnumerator ReturnAfterLifetime(Pool pool, GameObject instance)
        {
            yield return new WaitForSeconds(effectLifetime);
            pool.Return(instance);
        }

        /// <summary>A fixed set of instances handed out and taken back, never destroyed.</summary>
        private class Pool
        {
            private readonly Queue<GameObject> idle = new Queue<GameObject>();
            private readonly List<GameObject> all = new List<GameObject>();
            private readonly GameObject prefab;
            private readonly Transform parent;

            public Pool(GameObject prefab, int size, Transform parent)
            {
                this.prefab = prefab;
                this.parent = parent;
                if (prefab == null) return;

                for (int i = 0; i < size; i++)
                    idle.Enqueue(CreateInstance());
            }

            private GameObject CreateInstance()
            {
                GameObject instance = Object.Instantiate(prefab, parent);
                instance.SetActive(false);
                all.Add(instance);
                return instance;
            }

            public GameObject Get(Vector3 position)
            {
                if (prefab == null) return null;

                // Grow rather than fail, but log it so the starting size can be tuned.
                if (idle.Count == 0)
                {
                    Debug.LogWarning($"Pool for {prefab.name} ran dry, growing to {all.Count + 1}.");
                    idle.Enqueue(CreateInstance());
                }

                GameObject instance = idle.Dequeue();
                instance.transform.position = position;
                instance.SetActive(true);

                ParticleSystem particles = instance.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    particles.Clear();
                    particles.Play();
                }
                return instance;
            }

            public void Return(GameObject instance)
            {
                if (instance == null) return;
                instance.SetActive(false);
                idle.Enqueue(instance);
            }
        }
    }
}
