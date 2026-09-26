using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace HeadSoccer
{
    /// <summary>
    /// Object pooling for the short lived effects. A goal spawns dozens of particles and
    /// a match can produce hundreds of kick sparks, so Instantiate and Destroy would
    /// create garbage collection spikes. A dropped frame during a kick is an unfair miss,
    /// so every effect instance is created once up front and recycled forever.
    /// Built on Unity's <see cref="ObjectPool{T}"/>, pre-warmed so nothing is
    /// instantiated during play.
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

        /// <summary>
        /// A pre-warmed wrapper around Unity's ObjectPool: instances are created once on
        /// load, handed out on Get and deactivated on Return. The pool grows only if it
        /// ever runs dry, and logs it so the starting size can be tuned.
        /// </summary>
        private class Pool
        {
            private readonly ObjectPool<GameObject> pool;
            private readonly GameObject prefab;
            private readonly Transform parent;
            private bool warmedUp;

            public Pool(GameObject prefab, int size, Transform parent)
            {
                this.prefab = prefab;
                this.parent = parent;
                if (prefab == null) return;

                pool = new ObjectPool<GameObject>(
                    createFunc: Create,
                    actionOnGet: instance => instance.SetActive(true),
                    actionOnRelease: instance => instance.SetActive(false),
                    actionOnDestroy: Object.Destroy,
                    collectionCheck: false,
                    defaultCapacity: size,
                    maxSize: size * 4);

                // Pre-warm: allocate everything now, during the scene load, not mid-match.
                var warm = new GameObject[size];
                for (int i = 0; i < size; i++) warm[i] = pool.Get();
                for (int i = 0; i < size; i++) pool.Release(warm[i]);
                warmedUp = true;
            }

            private GameObject Create()
            {
                // After warm-up every new instance means the pool ran dry.
                if (warmedUp)
                    Debug.LogWarning($"Pool for {prefab.name} ran dry, growing to {pool.CountAll + 1}.");

                GameObject instance = Object.Instantiate(prefab, parent);
                instance.SetActive(false);
                return instance;
            }

            public GameObject Get(Vector3 position)
            {
                if (pool == null) return null;

                GameObject instance = pool.Get();
                instance.transform.position = position;

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
                if (instance == null || pool == null) return;
                pool.Release(instance);
            }
        }
    }
}
