using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Applies the kick impulse when the ball is inside kickRange at the moment the
    /// kick is pressed. It never teleports the ball and never fires out of range.
    /// Two things make every kick different, as in real football: the ball leaves the
    /// foot at a random angle (flat drive or lob, rolled fresh for each kick), and a
    /// player who arrives at speed hits harder than one standing still.
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
            Vector2 impulse = KickDirection() * config.kickImpulse * MomentumFactor() * powerMultiplier * boost;
            ballBody.AddForce(impulse, ForceMode2D.Impulse);

            AudioManager.Instance?.PlayKick();
            EffectsPool.Instance?.SpawnKickSpark(hit.transform.position);
            return true;
        }

        /// <summary>
        /// A fresh random angle for every kick, measured from the ground in the direction
        /// the player attacks: kickMinAngle is a flat drive, kickMaxAngle a lob.
        /// </summary>
        private Vector2 KickDirection()
        {
            float forward = owner != null ? owner.FacingDirection : 1f;
            float angle = Random.Range(config.kickMinAngle, config.kickMaxAngle) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle) * forward, Mathf.Sin(angle));
        }

        /// <summary>
        /// 1 when standing still, up to 1 + kickMomentumBonus when running at full speed
        /// toward the goal. Running away from the ball never weakens the kick below 1.
        /// </summary>
        private float MomentumFactor()
        {
            if (owner == null || config.moveSpeed <= 0f) return 1f;
            float runSpeed = owner.Velocity.x * owner.FacingDirection;
            float momentum = Mathf.Clamp01(runSpeed / config.moveSpeed);
            return 1f + config.kickMomentumBonus * momentum;
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, config.kickRange);
        }
    }
}
