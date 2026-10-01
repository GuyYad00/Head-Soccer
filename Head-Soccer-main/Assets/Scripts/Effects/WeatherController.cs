using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeadSoccer
{
    public enum MatchWeather { Clear, Rain, Snow }

    /// <summary>
    /// Rolls the weather once per match: half the matches are clear, a quarter play
    /// in rain and a quarter in snow. Purely visual - the chosen particle system
    /// runs over the pitch and the stadium painting takes a matching tint - so the
    /// ball and the players behave exactly the same in every weather.
    ///
    /// The controller installs itself when the match scene loads, and builds its two
    /// particle systems in code if the scene does not carry them, so it works with
    /// any Match.unity, including ones saved before the weather existed.
    /// </summary>
    public class WeatherController : MonoBehaviour
    {
        private const string MatchSceneName = "Match";
        private const float TopY = 7f;          // emission bar above the ceiling (CeilingY 5)
        private const float PitchWidth = 22f;   // covers the walls at +-8.9 plus the slant

        [Tooltip("Chance of a rain match. Clear weather takes whatever rain and snow leave.")]
        [Range(0f, 1f)] [SerializeField] private float rainChance = 0.25f;
        [Tooltip("Chance of a snow match.")]
        [Range(0f, 1f)] [SerializeField] private float snowChance = 0.25f;

        [Header("Scene pieces (built in code when left empty)")]
        [SerializeField] private ParticleSystem rain;
        [SerializeField] private ParticleSystem snow;
        [Tooltip("The stadium painting, tinted colder in rain and brighter in snow.")]
        [SerializeField] private SpriteRenderer stadium;
        [SerializeField] private Color rainStadiumTint = new Color(0.62f, 0.68f, 0.80f, 1f);
        [SerializeField] private Color snowStadiumTint = new Color(0.88f, 0.93f, 1.00f, 1f);

        public MatchWeather Weather { get; private set; }

        private static Material sharedMaterial;

        // --- self install -----------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnMatchLoad()
        {
            SceneManager.sceneLoaded += (scene, mode) => TryInstall(scene);
            TryInstall(SceneManager.GetActiveScene());   // the first scene is already loaded
        }

        private static void TryInstall(Scene scene)
        {
            if (scene.name != MatchSceneName) return;
            if (FindAnyObjectByType<WeatherController>() != null) return;   // scene already has one
            new GameObject("Weather").AddComponent<WeatherController>();
        }

        // --- the roll -----------------------------------------------------------

        private void Start()
        {
            if (rain == null) rain = BuildRain();
            if (snow == null) snow = BuildSnow();
            if (stadium == null)
            {
                GameObject painted = GameObject.Find("Stadium");
                if (painted != null) stadium = painted.GetComponent<SpriteRenderer>();
            }

            float roll = Random.value;
            if (roll < rainChance) Apply(MatchWeather.Rain);
            else if (roll < rainChance + snowChance) Apply(MatchWeather.Snow);
            else Apply(MatchWeather.Clear);
        }

        private void Apply(MatchWeather weather)
        {
            Weather = weather;

            if (rain != null)
            {
                if (weather == MatchWeather.Rain) rain.Play();
                else rain.gameObject.SetActive(false);
            }

            if (snow != null)
            {
                if (weather == MatchWeather.Snow) snow.Play();
                else snow.gameObject.SetActive(false);
            }

            if (stadium != null)
            {
                if (weather == MatchWeather.Rain) stadium.color = rainStadiumTint;
                else if (weather == MatchWeather.Snow) stadium.color = snowStadiumTint;
            }
        }

        // --- procedural particle systems ---------------------------------------
        // Mirrors what the scene builder makes, for scenes that predate the weather.

        private ParticleSystem BuildRain()
        {
            // Thin fast streaks with a slight slant, like rain against the floodlights.
            ParticleSystem system = CreateSystem("Rain", slantDegrees: 8f);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = 0.9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(16f, 20f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.07f);
            main.startColor = new Color(0.75f, 0.85f, 1f, 0.55f);
            main.maxParticles = 600;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 260f;

            // Stretched along the velocity, so each drop reads as a streak, not a dot.
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 7f;
            return system;
        }

        private ParticleSystem BuildSnow()
        {
            // Round flakes in mixed sizes, drifting on their way down.
            ParticleSystem system = CreateSystem("Snow", slantDegrees: 0f);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = 7f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new Color(1f, 1f, 1f, 0.9f);
            main.maxParticles = 500;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 55f;

            ParticleSystem.NoiseModule drift = system.noise;
            drift.enabled = true;
            drift.strength = 0.5f;
            drift.frequency = 0.25f;
            drift.scrollSpeed = 0.3f;
            return system;
        }

        /// <summary>
        /// A stopped particle system shaped as a horizontal bar above the ceiling,
        /// emitting straight down across the whole pitch, slanted for rain.
        /// </summary>
        private ParticleSystem CreateSystem(string name, float slantDegrees)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(0f, TopY, 0f);
            // Point the emitter down, then slant the fall by rotating around the world Z.
            go.transform.rotation = Quaternion.Euler(0f, 0f, slantDegrees) * Quaternion.Euler(90f, 0f, 0f);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(PitchWidth, 0.5f, 1f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = WeatherMaterial();
            renderer.sortingLayerName = "FX";
            return system;
        }

        private static Material WeatherMaterial()
        {
            if (sharedMaterial != null) return sharedMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                            ?? Shader.Find("Sprites/Default");
            sharedMaterial = new Material(shader) { mainTexture = CircleTexture(64) };
            return sharedMaterial;
        }

        /// <summary>A soft white disc, drawn in code so no art asset is needed.</summary>
        private static Texture2D CircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                                      new Vector2(radius, radius));
                    // Two pixels of soft edge so the disc is not jagged.
                    float alpha = Mathf.Clamp01((radius - distance) * 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
