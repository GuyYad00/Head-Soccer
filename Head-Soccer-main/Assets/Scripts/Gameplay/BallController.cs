using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Ball physics: a hard speed cap so it always stays trackable, a minimum speed so
    /// rallies never stall, a nudge back onto the pitch when it wedges in a goal,
    /// and a reset to the centre for kickoff.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Transform spawnPoint;

        // The goal line, in world units. Past it the ball is in a goal. Both goals
        // share this one number: the sign of the ball's x says which end it is in.
        [SerializeField] private float goalLineX = 7.7f;

        // A ball that sits still inside a goal is wedged on the frame. After this
        // long it is pushed back onto the pitch, or it stays there for the match.
        private const float StuckSeconds = 2f;
        private const float StuckSpeed = 0.5f;
        private const float EscapeSpeed = 8f;

        private Rigidbody2D rb;
        private Vector3 defaultSpawn;
        private float lastBounceSoundTime;
        private float stuckTime;

        // The minimum speed keeps a rally from dying. It must not run on the kickoff
        // drop, and it must not boost a ball that is still going up: both of those
        // turn a small bounce into a climb that gravity cannot cancel.
        private bool kickedSinceKickoff;

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

            bool wedged = Mathf.Abs(rb.position.x) >= goalLineX && speed < StuckSpeed;
            stuckTime = wedged ? stuckTime + Time.fixedDeltaTime : 0f;

            Vector2 escape = GoalEscape(rb.position.x, speed, stuckTime, goalLineX, StuckSeconds, StuckSpeed, EscapeSpeed);
            if (escape != Vector2.zero)
            {
                rb.WakeUp();
                rb.linearVelocity = escape;
                stuckTime = 0f;
                return;
            }

            if (speed > config.ballMaxSpeed)
                rb.linearVelocity = velocity.normalized * config.ballMaxSpeed;
            else if (kickedSinceKickoff && velocity.y <= 0f && speed > 0.05f && speed < config.ballMinSpeed)
                rb.linearVelocity = velocity.normalized * config.ballMinSpeed;
        }

        /// <summary>
        /// The push that frees a ball wedged in a goal. Both ends share one path:
        /// the sign of <paramref name="ballX"/> is the side, and the push points
        /// back to the pitch. Zero unless the ball has been past the goal line,
        /// and slower than <paramref name="stuckSpeed"/>, for the whole wait.
        /// </summary>
        public static Vector2 GoalEscape(float ballX, float speed, float stillSeconds, float goalLineX, float waitSeconds, float stuckSpeed, float pushSpeed)
        {
            if (stillSeconds < waitSeconds) return Vector2.zero;
            if (speed >= stuckSpeed) return Vector2.zero;
            if (Mathf.Abs(ballX) < goalLineX) return Vector2.zero;
            return new Vector2(-Mathf.Sign(ballX) * pushSpeed, 0f);
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
            kickedSinceKickoff = false;
            stuckTime = 0f;
            rb.position = defaultSpawn;
            rb.rotation = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        /// <summary>A connected kick. The rally floor applies from here, not from the drop.</summary>
        public void MarkKicked() => kickedSinceKickoff = true;

        /// <summary>Used for the kickoff countdown and the goal celebration freeze.</summary>
        public void Freeze()
        {
            stuckTime = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        public void Unfreeze()
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }
}
