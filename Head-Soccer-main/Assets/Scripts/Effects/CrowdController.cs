using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeadSoccer
{
    /// <summary>
    /// Country crowds in the stands: each half of the stadium fills with the home
    /// crowd of the character playing on that side, read from the choices saved by
    /// Character Select. Yossi and Noa bring the Israeli crowd, David the English,
    /// Kim the Japanese, Mikel the Nigerian and Anna the Ukrainian one.
    /// The controller installs itself when the match scene loads and lays one
    /// drawing over each half of the stands, so the scene needs nothing wired.
    /// The drawings stop under the stadium's own floodlights, and the crowd
    /// follows the stadium's weather tint so a rainy ground gets a rainy crowd.
    /// </summary>
    public class CrowdController : MonoBehaviour
    {
        private const string MatchSceneName = "Match";

        // The stands band of the stadium painting, in world units. The bottom edge
        // tucks behind the advertising board. The top stops short of the painted
        // floodlights, so those lamps are the only lights and nothing covers them.
        private const float HalfWidth = 9.25f;
        private const float BottomY = -1.0f;
        private const float TopY = 3.70f;

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

            // One drawing fills the half. It is scaled to the band, so the same
            // picture is never repeated and there is no seam down the middle.
            Vector2 tile = crowd.bounds.size;
            if (tile.x <= 0.001f || tile.y <= 0.001f) return renderer;

            float bandHeight = TopY - BottomY;
            half.transform.localScale = new Vector3(HalfWidth / tile.x, bandHeight / tile.y, 1f);
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
