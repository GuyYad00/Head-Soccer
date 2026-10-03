using System.Collections.Generic;
using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// The advertising board along the front of the stands, like the LED boards in a
    /// real stadium: one long banner strip scrolls slowly and wraps around forever.
    /// Each match draws one of the banner strips at random, with equal odds, so the
    /// ground is not dressed the same way twice in a row.
    /// Built from a few copies of the banner sprite laid end to end and clipped by a
    /// SpriteMask on this object, so the strip can be any length and never pops.
    /// Purely decorative: no colliders, and it sits on the Pitch sorting layer behind
    /// the players and the ball.
    /// </summary>
    [RequireComponent(typeof(SpriteMask))]
    public class AdBoard : MonoBehaviour
    {
        [Tooltip("The banner strips. One is drawn at random for each match.")]
        [SerializeField] private Sprite[] banners;
        [Tooltip("Visible width of the board in world units.")]
        [SerializeField] private float boardWidth = 18.5f;
        [Tooltip("Height of the board in world units; the banner keeps its aspect ratio.")]
        [SerializeField] private float boardHeight = 0.55f;
        [Tooltip("Scroll speed in world units per second. Negative scrolls to the left.")]
        [SerializeField] private float scrollSpeed = -0.6f;
        [SerializeField] private string sortingLayer = "Pitch";
        [SerializeField] private int sortingOrder = 5;

        private readonly List<Transform> tiles = new List<Transform>();
        private float tileWidth;

        private void Start()
        {
            Sprite banner = Draw(banners, Random.value);
            if (banner == null) return;

            // The mask clips the tiles to the board rectangle.
            var mask = GetComponent<SpriteMask>();
            mask.sprite = MaskSprite();
            transform.localScale = new Vector3(boardWidth, boardHeight, 1f);

            float scale = boardHeight / banner.bounds.size.y;
            tileWidth = banner.bounds.size.x * scale;
            int count = Mathf.CeilToInt(boardWidth / tileWidth) + 1;

            for (int i = 0; i < count; i++)
            {
                var tile = new GameObject("Banner " + i);
                tile.transform.SetParent(transform.parent, false);
                tile.transform.position = transform.position + new Vector3(-boardWidth * 0.5f + tileWidth * (i + 0.5f), 0f, 0f);
                tile.transform.localScale = new Vector3(scale, scale, 1f);

                var renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = banner;
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = sortingOrder;
                renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                tiles.Add(tile.transform);
            }
        }

        private void Update()
        {
            if (tiles.Count == 0) return;

            float step = scrollSpeed * Time.deltaTime;
            float leftEdge = transform.position.x - boardWidth * 0.5f - tileWidth * 0.5f;
            float rightEdge = transform.position.x + boardWidth * 0.5f + tileWidth * 0.5f;
            float loop = tileWidth * tiles.Count;

            for (int i = 0; i < tiles.Count; i++)
            {
                Vector3 p = tiles[i].position;
                p.x += step;
                // A tile that leaves one side reappears on the other, keeping the strip seamless.
                if (p.x < leftEdge) p.x += loop;
                else if (p.x > rightEdge) p.x -= loop;
                tiles[i].position = p;
            }
        }

        /// <summary>
        /// Picks the banner for this match. <paramref name="roll"/> is a number in
        /// [0, 1); each banner owns an equal slice of that range, so two banners are
        /// a coin toss and three are a fair die. Null when there is nothing to draw.
        /// </summary>
        public static Sprite Draw(Sprite[] banners, float roll)
        {
            if (banners == null || banners.Length == 0) return null;
            int index = Mathf.Clamp(Mathf.FloorToInt(roll * banners.Length), 0, banners.Length - 1);
            return banners[index];
        }

        /// <summary>A 1x1 white sprite so the mask is a plain rectangle scaled to the board.</summary>
        private static Sprite MaskSprite()
        {
            var texture = Texture2D.whiteTexture;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        }
    }
}
