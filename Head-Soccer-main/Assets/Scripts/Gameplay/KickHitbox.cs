using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Applies the kick impulse when the ball is inside kickRange at the moment the
    /// kick is pressed. It never teleports the ball and never fires out of range.
    /// Two things make every kick different, as in real football: the ball leaves the
    /// foot at a random angle (flat drive or lob, rolled fresh for each kick), and a
    /// player who arrives at speed hits harder than one standing still.
    /// A kick that lands on the rival also shoves him back a little, further when the
    /// kicker arrives at speed, so two players kicking at each other never lock up
    /// with the ball wedged between them.
    /// </summary>
    public class KickHitbox : MonoBehaviour
    {
        private const int MaxOverlaps = 8;

        [SerializeField] private GameConfig config;
        [SerializeField] private LayerMask ballLayer;

        private readonly Collider2D[] overlaps = new Collider2D[MaxOverlaps];
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
            ShoveRival();

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
        /// If the rival stands inside kick range, push him away from the kicker. The
        /// shove is small on purpose: enough to open a gap so the ball can move again,
        /// not enough to become a weapon. A standing kick gives kickPushback; a running
        /// kick scales it by the same MomentumFactor as the ball, so the shove follows
        /// the run the player came in with rather than a dice roll.
        /// </summary>
        private void ShoveRival()
        {
            if (owner == null || config.kickPushback <= 0f) return;

            int count = Physics2D.OverlapCircle(transform.position, config.kickRange, ContactFilter2D.noFilter, overlaps);
            for (int i = 0; i < count; i++)
            {
                var rival = overlaps[i].GetComponentInParent<PlayerController>();
                if (rival == null || rival == owner) continue;

                float away = Mathf.Sign(rival.transform.position.x - owner.transform.position.x);
                if (away == 0f) away = owner.FacingDirection;
                rival.Shove(away * config.kickPushback * MomentumFactor(), config.kickPushbackLift);
            }
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
