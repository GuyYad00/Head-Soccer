using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Every tuning number for the game lives here so it can be changed from the
    /// Inspector without recompiling. Values match the GDD tuning table.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Head Soccer/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Character movement")]
        public float moveSpeed = 6f;
        public float jumpVelocity = 12f;
        public float gravityScale = 3f;

        [Header("Kick")]
        public float kickImpulse = 14f;
        public float kickRange = 1.2f;
        [Tooltip("How much the kick pushes the ball upwards instead of straight forward.")]
        public float kickUpwardBias = 0.55f;
        [Tooltip("Seconds before the same character may kick again.")]
        public float kickCooldown = 0.25f;

        [Header("Ball")]
        public float ballBounciness = 0.7f;
        public float ballMaxSpeed = 22f;
        [Tooltip("Below this speed the ball gets a small nudge so rallies never stall.")]
        public float ballMinSpeed = 1.5f;
        public float ballGravityScale = 2.2f;

        [Header("Match")]
        public float matchLength = 90f;
        [Tooltip("Reaching this many goals ends the match early.")]
        public int goalTarget = 5;
        [Tooltip("Seconds of frozen celebration after a goal, before the next kickoff.")]
        public float goalFreezeTime = 1.5f;
        public int kickoffCountFrom = 3;

        [Header("Special shot")]
        [Tooltip("Seconds of ball contact needed to fill the Super meter from empty.")]
        public float superChargeTime = 6f;
        [Tooltip("Meter gained by one connecting kick (0..1).")]
        public float superKickCharge = 0.12f;
        [Tooltip("Meter gained when the ball bounces off the character (0..1).")]
        public float superBounceCharge = 0.08f;
        public float specialImpulseMultiplier = 2.2f;
        public float specialSlowMotionTime = 0.35f;
        [Tooltip("Brief freeze on a connecting kick so the hit reads clearly.")]
        public float kickHitStop = 0.05f;

        [Header("Feel")]
        [Tooltip("Extra time after leaving the ground where a jump still works.")]
        public float coyoteTime = 0.08f;
        [Tooltip("How long a jump press is remembered while airborne.")]
        public float jumpBufferTime = 0.12f;
        [Tooltip("Input is ignored for this long on the match over screen.")]
        public float matchOverInputLockout = 0.5f;
    }
}
