using System.Collections.Generic;
using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// The advertising board along the front of the stands, like the LED boards in a
    /// real stadium: one long banner strip scrolls slowly and wraps around forever.
    ///
    /// The adverts ship as single drawings in Resources/Ads. Every match shuffles all
    /// of them (each one with the same chance of every place) and paints them side by
    /// side into that one strip, fitted to the board. Five of them fill the screen,
    /// and because each advert is painted once, the five on screen are always five
    /// different ones. They are one texture on purpose: the board's SpriteMask draws
    /// every masked sprite from a single texture, so separate textures come out as
    /// copies of one advert.
    ///
    /// Purely decorative: no colliders, and it sits on the Pitch sorting layer behind
    /// the players and the ball.
    /// </summary>
    [RequireComponent(typeof(SpriteMask))]
    public class AdBoard : MonoBehaviour
    {
        private const int VisibleAds = 5;
        private const int SlotPixelsHigh = 180;

        [Tooltip("Visible width of the board in world units.")]
        [SerializeField] private float boardWidth = 18.5f;
        [Tooltip("Height of the board in world units.")]
        [SerializeField] private float boardHeight = 0.55f;
        [Tooltip("Scroll speed in world units per second. Negative scrolls to the left.")]
        [SerializeField] private float scrollSpeed = -0.6f;
        [SerializeField] private string sortingLayer = "Pitch";
        [SerializeField] private int sortingOrder = 5;

        private readonly List<Transform> tiles = new List<Transform>();
        private float tileWidth;
        private Texture2D stripTexture;
        private Sprite stripSprite;
        private Material tileMaterial;

        private void Start()
        {
            // The mask clips the tiles to the board rectangle.
            var mask = GetComponent<SpriteMask>();
            mask.sprite = MaskSprite();
            transform.localScale = new Vector3(boardWidth, boardHeight, 1f);

            List<Sprite> ads = LoadAds();
            if (ads.Count == 0)
            {
                Debug.LogWarning("AdBoard: no ad sprites loaded from Resources/Ads, the board stays empty.", this);
                return;
            }
            Debug.Log("AdBoard: painting " + ads.Count + " ads from "
                      + DistinctTextures(ads) + " textures.");

            // A fresh deal every match: every ad has the same chance of every place.
            for (int i = ads.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (ads[i], ads[j]) = (ads[j], ads[i]);
            }

            float slotWidth = boardWidth / VisibleAds;
            stripSprite = BuildStrip(ads, slotWidth);

            // The same conveyor the board always used: copies of one strip laid end to
            // end. The strip is twelve slots long and five are on screen, so a copy
            // never brings a repeated advert into view.
            float scale = boardHeight / stripSprite.bounds.size.y;
            tileWidth = stripSprite.bounds.size.x * scale;
            int count = Mathf.CeilToInt(boardWidth / tileWidth) + 1;
            tileMaterial = TileMaterial();

            for (int i = 0; i < count; i++)
            {
                var tile = new GameObject("Banner " + i);
                tile.transform.SetParent(transform.parent, false);
                tile.transform.position = transform.position + new Vector3(-boardWidth * 0.5f + tileWidth * (i + 0.5f), 0f, 0f);
                tile.transform.localScale = new Vector3(scale, scale, 1f);

                var renderer = tile.AddComponent<SpriteRenderer>();
                renderer.material = tileMaterial;
                renderer.sprite = stripSprite;
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = sortingOrder;
                renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                tiles.Add(tile.transform);
            }
        }

        private void OnDestroy()
        {
            if (stripSprite != null) Destroy(stripSprite);
            if (stripTexture != null) Destroy(stripTexture);
            if (tileMaterial != null) Destroy(tileMaterial);
        }

        private static int DistinctTextures(List<Sprite> ads)
        {
            var seen = new HashSet<int>();
            foreach (Sprite ad in ads) seen.Add(ad.texture.GetInstanceID());
            return seen.Count;
        }

        /// <summary>The twelve ad drawings, by file name, so a stray import can't add or drop one.</summary>
        private static List<Sprite> LoadAds()
        {
            var ads = new List<Sprite>();
            for (int i = 1; i <= 12; i++)
            {
                Sprite ad = Resources.Load<Sprite>("Ads/ad_" + i.ToString("00"));
                if (ad != null) ads.Add(ad);
            }
            return ads;
        }

        /// <summary>
        /// One banner with every advert stretched into an equal slot. Painting them
        /// into the pixels, rather than assigning twelve textures, is what keeps them
        /// visually distinct under the board's mask.
        /// </summary>
        private Sprite BuildStrip(List<Sprite> ads, float slotWorldWidth)
        {
            int slotPixelsWide = Mathf.Max(1, Mathf.RoundToInt(SlotPixelsHigh * slotWorldWidth / boardHeight));
            stripTexture = new Texture2D(slotPixelsWide * ads.Count, SlotPixelsHigh, TextureFormat.RGBA32, false);
            stripTexture.wrapMode = TextureWrapMode.Clamp;
            stripTexture.filterMode = FilterMode.Bilinear;

            RenderTexture previous = RenderTexture.active;
            for (int i = 0; i < ads.Count; i++)
            {
                Sprite ad = ads[i];
                var rt = RenderTexture.GetTemporary(slotPixelsWide, SlotPixelsHigh, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                // Sample this sprite's own rectangle, so a shared atlas page still
                // contributes only this one advert.
                var scale = new Vector2(ad.textureRect.width / ad.texture.width,
                                        ad.textureRect.height / ad.texture.height);
                var offset = new Vector2(ad.textureRect.x / ad.texture.width,
                                         ad.textureRect.y / ad.texture.height);
                Graphics.Blit(ad.texture, rt, scale, offset);
                RenderTexture.active = rt;
                stripTexture.ReadPixels(new Rect(0, 0, slotPixelsWide, SlotPixelsHigh), i * slotPixelsWide, 0);
                RenderTexture.ReleaseTemporary(rt);
            }
            stripTexture.Apply(false, false);
            RenderTexture.active = previous;

            float pixelsPerUnit = SlotPixelsHigh / boardHeight;
            return Sprite.Create(stripTexture,
                new Rect(0, 0, stripTexture.width, stripTexture.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        /// <summary>
        /// An unlit sprite material. A texture built at runtime can come out invisible
        /// under URP's 2D lights when it keeps the default material.
        /// </summary>
        private static Material TileMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                            ?? Shader.Find("Sprites/Default");
            return new Material(shader);
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

        /// <summary>A 1x1 white sprite so the mask is a plain rectangle scaled to the board.</summary>
        private static Sprite MaskSprite()
        {
            var texture = Texture2D.whiteTexture;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        }
    }
}
