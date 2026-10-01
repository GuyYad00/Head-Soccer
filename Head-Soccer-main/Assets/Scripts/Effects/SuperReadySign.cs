using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// A glowing aura around the whole character while the Super is ready, instead
    /// of the old bang mark: a soft ball of light behind the player plus the
    /// player's own drawing enlarged and tinted electric blue, both pulsing until
    /// the shot is spent. Builds itself at runtime, so the existing Player prefab
    /// works as it is.
    /// </summary>
    public class SuperReadySign : MonoBehaviour
    {
        private static readonly Color GlowColor = new Color(0.25f, 0.80f, 1f, 0.55f);
        private static readonly Color RimColor = new Color(0.45f, 0.95f, 1f, 0.9f);

        [SerializeField] private PlayerController player;
        [Tooltip("The old bang mark; kept so older prefabs stay wired, never shown.")]
        [SerializeField] private Transform mark;

        private SpriteRenderer source;   // the character drawing the rim copies
        private SpriteRenderer glow;     // the soft ball of light behind the player
        private SpriteRenderer rim;      // the enlarged tinted copy of the drawing

        private static Material auraMaterial;
        private static Sprite glowSprite;

        public void Setup(PlayerController owner, Transform sign)
        {
            player = owner;
            mark = sign;
            if (mark != null) mark.gameObject.SetActive(false);
        }

        private void Start()
        {
            // The bang mark is retired; prefabs that still carry it keep it hidden.
            if (mark != null) mark.gameObject.SetActive(false);

            var visual = GetComponentInChildren<PlayerVisual>();
            if (visual != null) source = visual.GetComponent<SpriteRenderer>();
            if (source == null) return;

            // The ball of light sits on the player root, so it keeps its shape
            // through flips and kick squashes. Centred on the body.
            var glowObject = new GameObject("SuperAuraGlow");
            glowObject.transform.SetParent(transform, false);
            glowObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            glow = glowObject.AddComponent<SpriteRenderer>();
            glow.sprite = GlowSprite();
            glow.material = AuraMaterial();
            glow.sortingLayerName = source.sortingLayerName;
            glow.sortingOrder = source.sortingOrder - 2;
            glow.color = GlowColor;
            glow.enabled = false;

            // The rim is parented to the drawing itself, so it inherits the flip,
            // the squash and the kick pose, and only adds its own extra size.
            var rimObject = new GameObject("SuperAuraRim");
            rimObject.transform.SetParent(source.transform, false);
            rim = rimObject.AddComponent<SpriteRenderer>();
            rim.material = AuraMaterial();
            rim.sortingLayerName = source.sortingLayerName;
            rim.sortingOrder = source.sortingOrder - 1;
            rim.color = RimColor;
            rim.enabled = false;
        }

        private void LateUpdate()
        {
            if (player == null || glow == null || rim == null || source == null) return;

            bool show = player.SpecialReady;
            if (glow.enabled != show) glow.enabled = show;
            if (rim.enabled != show) rim.enabled = show;
            if (!show) return;

            // The rim wears the current drawing, so it hugs the pose frame by frame.
            if (rim.sprite != source.sprite) rim.sprite = source.sprite;

            // A slow breath: the aura swells and brightens, then settles.
            float wave = Mathf.Sin(Time.unscaledTime * 6f);

            float rimGrow = 1.18f + wave * 0.05f;
            rim.transform.localScale = new Vector3(rimGrow, rimGrow, 1f);
            Color rimNow = RimColor;
            rimNow.a = 0.65f + Mathf.Abs(wave) * 0.3f;
            rim.color = rimNow;

            float glowGrow = 1f + Mathf.Abs(wave) * 0.12f;
            glow.transform.localScale = new Vector3(2.6f * glowGrow, 3.1f * glowGrow, 1f);
            Color glowNow = GlowColor;
            glowNow.a = 0.4f + Mathf.Abs(wave) * 0.25f;
            glow.color = glowNow;
        }

        /// <summary>
        /// An unlit sprite material that URP's 2D renderer always draws; the default
        /// runtime material can come out invisible under a 2D light setup.
        /// </summary>
        private static Material AuraMaterial()
        {
            if (auraMaterial != null) return auraMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                            ?? Shader.Find("Sprites/Default");
            auraMaterial = new Material(shader);
            return auraMaterial;
        }

        /// <summary>A soft radial gradient disc, drawn in code so no art asset is needed.</summary>
        private static Sprite GlowSprite()
        {
            if (glowSprite != null) return glowSprite;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                                      new Vector2(radius, radius));
                    // Quadratic falloff: bright core, feathered edge.
                    float fade = Mathf.Clamp01(1f - distance / radius);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, fade * fade);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            // Pixels-per-unit equal to the size makes the sprite exactly one world
            // unit, so the transform scale reads directly as size in units.
            glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                                       new Vector2(0.5f, 0.5f), size);
            return glowSprite;
        }
    }
}
