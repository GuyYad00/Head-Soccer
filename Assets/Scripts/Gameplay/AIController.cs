using UnityEngine;

namespace HeadSoccer
{
    public enum AIDifficulty { Easy, Medium, Hard }

    /// <summary>
    /// Drives a character by producing the same three inputs a human produces.
    /// It moves toward the ball, keeps itself on the goal side of the ball so its
    /// kicks travel forward, jumps at high balls and kicks when the ball is in range.
    /// </summary>
    public class AIController : IInputSource
    {
        private readonly GameConfig config;
        private readonly Transform self;
        private readonly float attackDirection;   // +1 attacks right, -1 attacks left
        private readonly float homeX;

        private readonly float reactionTime;
        private readonly float sloppiness;
        private readonly float aggression;

        private float nextDecisionTime;
        private float targetX;
        private bool wantsJump;
        private bool wantsKick;
        private bool wantsSuper;

        public float Horizontal { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool KickPressedThisFrame { get; private set; }
        public bool SuperPressedThisFrame { get; private set; }

        public AIController(GameConfig config, Transform self, Side side, AIDifficulty difficulty, float homeX)
        {
            this.config = config;
            this.self = self;
            this.homeX = homeX;
            attackDirection = side == Side.Left ? 1f : -1f;

            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    reactionTime = 0.28f; sloppiness = 1.2f; aggression = 0.55f; break;
                case AIDifficulty.Hard:
                    reactionTime = 0.06f; sloppiness = 0.15f; aggression = 1f; break;
                default:
                    reactionTime = 0.15f; sloppiness = 0.5f; aggression = 0.8f; break;
            }
        }

        public void Poll()
        {
            JumpPressedThisFrame = false;
            KickPressedThisFrame = false;
            SuperPressedThisFrame = false;

            BallController ball = GameManager.Instance != null ? GameManager.Instance.Ball : null;
            if (ball == null)
            {
                Horizontal = 0f;
                return;
            }

            Vector2 ballPos = ball.transform.position;
            Vector2 myPos = self.position;

            if (Time.time >= nextDecisionTime)
            {
                nextDecisionTime = Time.time + reactionTime;
                Decide(ballPos, myPos, ball);
            }

            float distanceToTarget = targetX - myPos.x;
            Horizontal = Mathf.Abs(distanceToTarget) < 0.15f ? 0f : Mathf.Sign(distanceToTarget);

            if (wantsJump)
            {
                JumpPressedThisFrame = true;
                wantsJump = false;
            }

            if (wantsKick)
            {
                KickPressedThisFrame = true;
                wantsKick = false;
            }

            if (wantsSuper)
            {
                SuperPressedThisFrame = true;
                wantsSuper = false;
            }
        }

        private void Decide(Vector2 ballPos, Vector2 myPos, BallController ball)
        {
            // Predict a little ahead so the CPU does not always chase where the ball was.
            Vector2 predicted = ballPos + ball.Velocity * (reactionTime * 2f);
            float noise = Random.Range(-sloppiness, sloppiness);

            bool ballIsOnMySide = (predicted.x - homeX) * attackDirection < 0f;
            if (ballIsOnMySide || Random.value < aggression)
            {
                // Stand slightly behind the ball, on the goal side, so a kick sends it forward.
                targetX = predicted.x - attackDirection * 0.45f + noise;
            }
            else
            {
                targetX = homeX + noise;   // hang back and defend
            }

            float horizontalGap = Mathf.Abs(ballPos.x - myPos.x);
            float verticalGap = ballPos.y - myPos.y;

            wantsJump = horizontalGap < config.kickRange * 1.6f
                        && verticalGap > 0.9f
                        && Random.value < aggression;

            wantsKick = Vector2.Distance(ballPos, myPos) < config.kickRange * 0.95f
                        && Random.value < 0.5f + aggression * 0.5f;

            var player = self.GetComponent<PlayerController>();
            wantsSuper = player != null
                         && player.SpecialReady
                         && Vector2.Distance(ballPos, myPos) < config.kickRange
                         && Random.value < 0.55f;
        }
    }
}
