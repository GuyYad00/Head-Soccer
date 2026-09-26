using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Applies the kick impulse when the ball is inside kickRange at the moment the
    /// kick is pressed. It never teleports the ball and never fires out of range.
    /// </summary>
    public class KickHitbox : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private LayerMask ballLayer;

        private PlayerController owner;
        private float powerMultiplier = 1f;

        public void Configure(PlayerController player, float power)
        {
            owner = player;
            powerMultiplier = power;
        }

        /// <summary>Returns true only if the kick actually connected with the ball.</summary>
        public bool TryKick(bool special = false)
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, config.kickRange, ballLayer);
            if (hit == null) return false;

            Rigidbody2D ballBody = hit.attachedRigidbody;
            if (ballBody == null) return false;

            float boost = special ? config.specialImpulseMultiplier : 1f;
            Vector2 impulse = BuildImpulse(hit.transform.position) * powerMultiplier * boost;
            ballBody.AddForce(impulse, ForceMode2D.Impulse);

            AudioManager.Instance?.PlayKick();
            EffectsPool.Instance?.SpawnKickSpark(hit.transform.position);
            return true;
        }

        private Vector2 BuildImpulse(Vector2 ballPosition)
        {
            Vector2 toBall = ballPosition - (Vector2)transform.position;
            Vector2 direction = toBall.sqrMagnitude < 0.0001f ? Vector2.up : toBall.normalized;

            float forward = owner != null ? owner.FacingDirection : 1f;
            direction = (direction
                         + Vector2.right * forward
                         + Vector2.up * config.kickUpwardBias).normalized;

            return direction * config.kickImpulse;
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, config.kickRange);
        }
    }
}
