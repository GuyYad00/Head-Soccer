using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// The character drawing. Flips with the player's facing, squashes on a kick and
    /// stretches in the air, and can swap its sprite for the chosen character.
    /// If the character has a kick pose drawing, it is shown for the kick window.
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        private const float KickPoseSeconds = 0.18f;

        [SerializeField] private PlayerController player;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool spriteFacesRight = true;

        private Sprite idleSprite;
        private Sprite kickSprite;
        private Vector3 baseScale;
        private float kickPoseUntil;

        public void PlayKickPose()
        {
            kickPoseUntil = Time.time + KickPoseSeconds;
        }

        /// <summary>Swaps the drawing for the selected character, keeping the world height.</summary>
        public void ApplyLook(Sprite idle, Sprite kick, bool facesRight, Color tint)
        {
            if (spriteRenderer == null) return;
            spriteFacesRight = facesRight;

            if (idle != null && spriteRenderer.sprite != null && spriteRenderer.sprite != idle)
            {
                float currentHeight = spriteRenderer.sprite.bounds.size.y * Mathf.Abs(transform.localScale.y);
                float scale = currentHeight / Mathf.Max(0.001f, idle.bounds.size.y);
                transform.localScale = new Vector3(scale, scale, 1f);
                baseScale = transform.localScale;
            }

            if (idle != null)
            {
                idleSprite = idle;
                spriteRenderer.sprite = idle;
            }
            kickSprite = kick;
            spriteRenderer.color = tint.a > 0f ? tint : Color.white;
        }

        private void Awake()
        {
            baseScale = transform.localScale;
            if (spriteRenderer != null && idleSprite == null)
                idleSprite = spriteRenderer.sprite;
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

            if (spriteRenderer == null || kickSprite == null) return;
            Sprite wanted = kicking ? kickSprite : idleSprite;
            if (spriteRenderer.sprite != wanted) spriteRenderer.sprite = wanted;
        }
    }
}
