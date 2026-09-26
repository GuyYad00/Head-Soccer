using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Auto is the normal setting: the left character takes the human controls and the
    /// right one becomes either player two or the CPU depending on MatchSettings.
    /// </summary>
    public enum InputMode { Auto, KeyboardP1, KeyboardP2, Touch, AI }

    /// <summary>
    /// Move, jump and kick for one character. Input is read in Update and applied in
    /// FixedUpdate, so a press is never dropped between two physics steps.
    /// The Super meter itself lives in <see cref="SpecialShot"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig config;
        [SerializeField] private Side side = Side.Left;
        [SerializeField] private InputMode inputMode = InputMode.Auto;
        [SerializeField] private AIDifficulty aiDifficulty = AIDifficulty.Medium;

        [Header("References")]
        [SerializeField] private TouchInputSource touchSource;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private KickHitbox kickHitbox;
        [SerializeField] private PlayerVisual visual;

        [Header("Character")]
        [Tooltip("When set, the character chosen on the menu overrides the multipliers below.")]
        [SerializeField] private CharacterRoster roster;
        [SerializeField, Range(0.6f, 1.6f)] private float speedMultiplier = 1f;
        [SerializeField, Range(0.6f, 1.6f)] private float jumpMultiplier = 1f;
        [SerializeField, Range(0.6f, 1.6f)] private float powerMultiplier = 1f;

        private const float GroundCheckRadius = 0.16f;

        private Rigidbody2D rb;
        private IInputSource input;
        private SpecialShot special;
        private Vector3 spawnPosition;

        private float moveInput;
        private float jumpBufferedUntil = -1f;
        private float lastGroundedTime = -1f;
        private bool kickBuffered;
        private bool superBuffered;
        private float nextKickAllowedTime;

        public Side Side => side;
        public bool IsGrounded { get; private set; }
        public float HorizontalInput => moveInput;
        public string CharacterName { get; private set; } = string.Empty;
        public float SpecialCharge => special?.Charge ?? 0f;
        public bool SpecialReady => special != null && special.IsReady;
        public bool SpecialUsed => special != null && special.IsUsed;
        /// <summary>+1 for the left character (it attacks right), -1 for the right one.</summary>
        public float FacingDirection => side == Side.Left ? 1f : -1f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = config.gravityScale;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.linearDamping = 0f;

            spawnPosition = transform.position;
            special = new SpecialShot(config);

            ApplyCharacter();
            input = CreateInputSource();

            if (kickHitbox != null)
                kickHitbox.Configure(this, powerMultiplier);
        }

        /// <summary>Loads the menu choice: player one gets the chosen character, the other side its rival.</summary>
        private void ApplyCharacter()
        {
            if (roster == null || roster.Count == 0) return;

            CharacterDefinition character = side == Side.Left ? roster.Selected : roster.Opponent;
            speedMultiplier = character.speed;
            jumpMultiplier = character.jump;
            powerMultiplier = character.power;
            CharacterName = character.displayName;

            if (visual != null)
                visual.ApplyLook(character.portrait, character.kickPose, character.facesRight, character.tint);
        }

        private IInputSource CreateInputSource()
        {
            switch (inputMode)
            {
                case InputMode.KeyboardP1: return KeyboardInputSource.PlayerOne();
                case InputMode.KeyboardP2: return KeyboardInputSource.PlayerTwo();
                case InputMode.Touch:
                    return touchSource != null ? (IInputSource)touchSource : KeyboardInputSource.PlayerOne();
                case InputMode.AI: return CreateAI(aiDifficulty);
                default: return ResolveAutoInput();
            }
        }

        private IInputSource ResolveAutoInput()
        {
            // Nothing moves unless a key is held. CPU is only used when the menu
            // explicitly starts a 1P match.
            if (side == Side.Left)
            {
                if (Application.isMobilePlatform && touchSource != null)
                    return touchSource;
                return new CompositeInputSource(KeyboardInputSource.PlayerOne(), new GamepadInputSource(0));
            }

            if (MatchSettings.Mode == GameMode.OnePlayerVsCPU)
                return CreateAI(MatchSettings.Difficulty);

            return new CompositeInputSource(KeyboardInputSource.PlayerTwo(), new GamepadInputSource(1));
        }

        public void RebuildInput()
        {
            input = CreateInputSource();
        }

        private IInputSource CreateAI(AIDifficulty difficulty) =>
            new AIController(config, transform, side, difficulty, transform.position.x);

        private void Update()
        {
            bool active = GameManager.Instance == null || GameManager.Instance.ControlsEnabled;
            if (!active)
            {
                moveInput = 0f;
                kickBuffered = false;
                superBuffered = false;
                jumpBufferedUntil = -1f;
                return;
            }

            input.Poll();
            moveInput = Mathf.Clamp(input.Horizontal, -1f, 1f);

            if (input.JumpPressedThisFrame)
                jumpBufferedUntil = Time.time + config.jumpBufferTime;

            if (input.KickPressedThisFrame)
                kickBuffered = true;

            if (input.SuperPressedThisFrame)
                superBuffered = true;
        }

        private void FixedUpdate()
        {
            bool active = GameManager.Instance == null || GameManager.Instance.ControlsEnabled;

            IsGrounded = Physics2D.OverlapCircle(groundCheck.position, GroundCheckRadius, groundLayer);
            if (IsGrounded) lastGroundedTime = Time.time;

            if (!active)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            float xSpeed = moveInput * config.moveSpeed * speedMultiplier;
            if (Mathf.Abs(moveInput) < 0.01f)
                xSpeed = 0f;

            rb.linearVelocity = new Vector2(xSpeed, rb.linearVelocity.y);

            bool jumpRequested = Time.time <= jumpBufferedUntil;
            bool canJump = Time.time - lastGroundedTime <= config.coyoteTime;
            if (jumpRequested && canJump)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x,
                                                config.jumpVelocity * jumpMultiplier);
                jumpBufferedUntil = -1f;
                lastGroundedTime = -1f;      // no double jump
                AudioManager.Instance?.PlayJump();
            }

            ChargeFromProximity();

            // Either the dedicated Super input or a plain kick fires the Super once it
            // is ready. That keeps the touch layout at four buttons, as the GDD asks.
            bool wantsSuper = superBuffered || (kickBuffered && SpecialReady);
            superBuffered = false;

            if (wantsSuper && SpecialReady)
            {
                kickBuffered = false;
                TrySpecial();
                return;
            }

            if (kickBuffered)
            {
                kickBuffered = false;
                if (visual != null) visual.PlayKickPose();

                if (Time.time >= nextKickAllowedTime && kickHitbox != null && kickHitbox.TryKick())
                {
                    nextKickAllowedTime = Time.time + config.kickCooldown;
                    special.ChargeFromKick();
                    GameManager.Instance?.PlayKickHitStop();
                }
            }
        }

        private void TrySpecial()
        {
            if (!SpecialReady || kickHitbox == null) return;

            if (visual != null) visual.PlayKickPose();
            // Only a connecting shot spends the meter, so whiffing is not punished.
            if (!kickHitbox.TryKick(special: true)) return;

            special.TryFire();
            AudioManager.Instance?.PlaySpecial();
            GameManager.Instance?.PlaySpecialWindow();
        }

        /// <summary>Being on the ball is how the meter fills; standing far away earns nothing.</summary>
        private void ChargeFromProximity()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != MatchState.Playing) return;

            BallController ball = GameManager.Instance.Ball;
            if (ball == null) return;

            float reach = config.kickRange * 1.6f;
            if (Vector2.Distance(ball.transform.position, transform.position) <= reach)
                special.ChargeFromContact(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponentInParent<BallController>() == null) return;
            special.ChargeFromBounce();
        }

        public void ResetToSpawn()
        {
            transform.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            moveInput = 0f;
            kickBuffered = false;
            superBuffered = false;
            jumpBufferedUntil = -1f;
        }

        /// <summary>New match, new Super.</summary>
        public void ResetSpecial() => special?.Reset();

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, GroundCheckRadius);
        }
    }
}
