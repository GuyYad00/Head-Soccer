using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Flips the whole visual rig (head plus tiny body) with the player's facing.
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool spriteFacesRight = true;

        private Vector3 baseScale;
        private float kickPoseUntil;

        public void PlayKickPose()
        {
            kickPoseUntil = Time.time + 0.18f;
        }

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void LateUpdate()
        {
            if (player == null) return;

            float dir = player.FacingDirection;
            if (Mathf.Abs(player.HorizontalInput) > 0.15f)
                dir = Mathf.Sign(player.HorizontalInput);

            bool wantRight = dir >= 0f;
            float face = spriteFacesRight == wantRight ? 1f : -1f;

            bool kicking = Time.time < kickPoseUntil;
            float squash = kicking ? 1.08f : 1f;
            float stretch = kicking ? 0.94f : (player.IsGrounded ? 1f : 1.06f);

            transform.localScale = new Vector3(
                Mathf.Abs(baseScale.x) * face * squash,
                Mathf.Abs(baseScale.y) * stretch,
                1f);
        }
    }
}
