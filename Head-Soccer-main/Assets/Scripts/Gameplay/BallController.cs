using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Ball physics: a hard speed cap so it always stays trackable, a minimum speed so
    /// rallies never stall, and a reset to the centre for kickoff.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Transform spawnPoint;

        private Rigidbody2D rb;
        private Vector3 defaultSpawn;
        private float lastBounceSoundTime;

        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = config.ballGravityScale;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            // Without continuous detection a 22 u/s ball tunnels straight through the net.
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            defaultSpawn = spawnPoint != null ? spawnPoint.position : transform.position;
        }

        private void FixedUpdate()
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic) return;

            Vector2 velocity = rb.linearVelocity;
            float speed = velocity.magnitude;

            if (speed > config.ballMaxSpeed)
                rb.linearVelocity = velocity.normalized * config.ballMaxSpeed;
            else if (speed > 0.05f && speed < config.ballMinSpeed)
                rb.linearVelocity = velocity.normalized * config.ballMinSpeed;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Rate limited so a ball rolling along the floor does not machine-gun the SFX.
            if (Time.time - lastBounceSoundTime < 0.08f) return;
            lastBounceSoundTime = Time.time;

            if (collision.relativeVelocity.magnitude > 2f)
                AudioManager.Instance?.PlayBounce();
        }

        public void ResetToCenter()
        {
            transform.position = defaultSpawn;
            transform.rotation = Quaternion.identity;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        /// <summary>Used for the kickoff countdown and the goal celebration freeze.</summary>
        public void Freeze()
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        public void Unfreeze()
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }
}
