using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeadSoccer
{
    /// <summary>
    /// Country crowds in the stands: each half of the stadium fills with the home
    /// crowd of the character playing on that side, read from the choices saved by
    /// Character Select. Yossi and Noa bring the Israeli crowd, David the English,
    /// Kim the Japanese, Mikel the Nigerian and Anna the Ukrainian one.
    /// The controller installs itself when the match scene loads and fills each
    /// half of the stands with that side's drawing, two tiers high up to the top
    /// of the screen, so the scene needs nothing wired. The crowd follows the
    /// stadium's weather tint so a rainy ground gets a rainy crowd.
    /// </summary>
    public class CrowdController : MonoBehaviour
    {
        private const string MatchSceneName = "Match";

        // The stands band, in world units. The bottom edge tucks behind the
        // advertising board. The top sits just above the camera's top edge (the
        // orthographic size is 5), so the crowd runs to the edge of the screen and
        // the floodlights painted on the stadium are hidden behind it.
        private const float HalfWidth = 9.25f;
        private const float BottomY = -1.0f;
        private const float TopY = 5.1f;

        // The stands are two tiers of supporters, one drawing per tier, each with
        // its flag banner along the front, so the banner of the upper tier reads
        // as the rail between them.
        private const int Tiers = 2;

        // Roster order is fixed by the builder: Yossi, David, Kim, Mikel, Noa, Anna.
        private static readonly string[] CountryByIndex =
            { "israel", "england", "japan", "nigeria", "israel", "ukraine" };

        private SpriteRenderer left;
        private SpriteRenderer right;
        private SpriteRenderer stadium;

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
            if (FindAnyObjectByType<CrowdController>() != null) return;
            new GameObject("Crowds").AddComponent<CrowdController>();
        }

        // --- the stands ---------------------------------------------------------

        private void Start()
        {
            GameObject painted = GameObject.Find("Stadium");
            if (painted != null) stadium = painted.GetComponent<SpriteRenderer>();

            left = BuildHalf("CrowdLeft", CharacterRoster.SelectedIndex, -1f);
            right = BuildHalf("CrowdRight", CharacterRoster.OpponentIndex, 1f);
        }

        private SpriteRenderer BuildHalf(string name, int characterIndex, float sign)
        {
            Sprite crowd = Resources.Load<Sprite>("Crowds/" + CountryFor(characterIndex));
            if (crowd == null) return null;

            var half = new GameObject(name);
            half.transform.SetParent(transform, false);
            half.transform.position = new Vector3(sign * HalfWidth * 0.5f, (TopY + BottomY) * 0.5f, 0f);

            var renderer = half.AddComponent<SpriteRenderer>();
            renderer.sprite = crowd;
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = 1;   // over the stadium painting, behind everything else

            // One drawing spans the whole width of the half, so there is no seam
            // down the middle, and it repeats once upward for the second tier.
            // The stack is then scaled to the band.
            Vector2 tile = crowd.bounds.size;
            if (tile.x <= 0.001f || tile.y <= 0.001f) return renderer;

            float bandHeight = TopY - BottomY;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(tile.x, tile.y * Tiers);
            half.transform.localScale = new Vector3(HalfWidth / renderer.size.x, bandHeight / renderer.size.y, 1f);
            return renderer;
        }

        private static string CountryFor(int index)
        {
            if (index < 0 || index >= CountryByIndex.Length) return CountryByIndex[0];
            return CountryByIndex[index];
        }

        private void LateUpdate()
        {
            // The weather tints the stadium painting; the crowds wear the same tint,
            // so a dark rainy ground never sits behind a bright sunny crowd.
            if (stadium == null) return;
            if (left != null && left.color != stadium.color) left.color = stadium.color;
            if (right != null && right.color != stadium.color) right.color = stadium.color;
        }
    }
}
