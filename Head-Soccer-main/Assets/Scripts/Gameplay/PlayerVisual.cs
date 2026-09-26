using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// The character drawing. Flips with the player's facing, squashes on a kick and
    /// stretches in the air, and can swap its sprite for the chosen character.
    /// If the character has a kick pose drawing, it is shown for the kick window.
    /// Every sprite is scaled to the same world height, so drawings of different
    /// pixel sizes line up with the collider.
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        private const float KickPoseSeconds = 0.18f;

        [SerializeField] private PlayerController player;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool spriteFacesRight = true;

        private Sprite idleSprite;
        private Sprite kickSprite;
        private float worldHeight;
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
            if (spriteRenderer == null) return;
            idleSprite = spriteRenderer.sprite;
            // The builder sized the idle drawing; remember that height for every sprite.
            if (idleSprite != null)
                worldHeight = idleSprite.bounds.size.y * Mathf.Abs(transform.localScale.y);
        }

        private float ScaleFor(Sprite sprite)
        {
            if (sprite == null || sprite.bounds.size.y < 0.001f) return Mathf.Abs(transform.localScale.y);
            return worldHeight / sprite.bounds.size.y;
        }

        private void LateUpdate()
        {
            if (player == null || spriteRenderer == null) return;

            float dir = player.FacingDirection;
            if (Mathf.Abs(player.HorizontalInput) > 0.15f)
                dir = Mathf.Sign(player.HorizontalInput);

            bool wantRight = dir >= 0f;
            float face = spriteFacesRight == wantRight ? 1f : -1f;

            bool kicking = Time.time < kickPoseUntil;
            Sprite wanted = kicking && kickSprite != null ? kickSprite : idleSprite;
            if (wanted != null && spriteRenderer.sprite != wanted) spriteRenderer.sprite = wanted;

            // Squash on a kick, stretch in the air; skipped when a real kick drawing is shown.
            bool poseDrawn = kicking && kickSprite != null;
            float squash = kicking && !poseDrawn ? 1.08f : 1f;
            float stretch = kicking && !poseDrawn ? 0.94f : (player.IsGrounded ? 1f : 1.06f);

            float scale = ScaleFor(spriteRenderer.sprite);
            transform.localScale = new Vector3(scale * face * squash, scale * stretch, 1f);
        }
    }
}
