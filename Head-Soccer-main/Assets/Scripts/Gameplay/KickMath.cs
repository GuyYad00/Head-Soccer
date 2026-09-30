using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// The arithmetic behind a kick, kept out of MonoBehaviour so it can be unit
    /// tested without a scene. KickHitbox owns the physics query and the random roll;
    /// this owns the numbers that turn them into an impulse.
    /// </summary>
    public static class KickMath
    {
        /// <summary>
        /// 1 when standing still, up to 1 + <paramref name="momentumBonus"/> when running
        /// at full <paramref name="moveSpeed"/> toward the goal. Running away from the
        /// ball never weakens the kick below 1, and speed above full speed gives no extra.
        /// </summary>
        public static float MomentumFactor(float runSpeedTowardGoal, float moveSpeed, float momentumBonus)
        {
            if (moveSpeed <= 0f) return 1f;
            float momentum = Mathf.Clamp01(runSpeedTowardGoal / moveSpeed);
            return 1f + momentumBonus * momentum;
        }

        /// <summary>
        /// Unit vector for a kick that leaves the foot <paramref name="angleDegrees"/>
        /// above the ground, in the direction the player attacks (+1 right, -1 left).
        /// 0 degrees is a flat drive, 90 straight up.
        /// </summary>
        public static Vector2 Direction(float angleDegrees, float forward)
        {
            float angle = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle) * Mathf.Sign(forward), Mathf.Sin(angle));
        }
    }
}
