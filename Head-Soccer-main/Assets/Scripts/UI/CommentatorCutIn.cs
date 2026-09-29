using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HeadSoccer
{
    /// <summary>
    /// The commentator cut-in. There is no football without a commentator, so one of
    /// the three is drawn at random when the match starts and stays for the whole
    /// match. On every goal his figure pops up in the crowd above the goal that just
    /// received the ball, he shouts the goal call over a muted game mix, and the moment
    /// the ball is back in play (kickoff whistle) he pops away and the call is cut.
    /// The figure is placed in world units and converted to the canvas every frame,
    /// so it stays inside the stadium on every aspect ratio and follows the camera
    /// shake instead of hanging in the black margin beside the pitch.
    /// Listens to GameManager events, so the manager never knows this exists; REMATCH
    /// reloads the scene and draws a new commentator.
    /// </summary>
    public class CommentatorCutIn : MonoBehaviour
    {
        [Tooltip("The commentator drawings. One is picked at random per match.")]
        [SerializeField] private Sprite[] commentators;
        [Tooltip("The figure that pops in and out: the portrait with the LIVE tag above it. Pivot at the bottom centre.")]
        [SerializeField] private RectTransform figure;
        [SerializeField] private Image portrait;

        [Header("Placement, world units")]
        [Tooltip("Distance from the pitch centre to the figure's centre. Mirrored to the side of the goal that received the ball.")]
        [SerializeField] private float goalSideX = 7.35f;
        [Tooltip("Height of the figure's feet: just above the crossbar, in the crowd.")]
        [SerializeField] private float standY = 0.45f;
        [Tooltip("Height of the whole figure, portrait plus tag. Kept under the ceiling of the pitch.")]
        [SerializeField] private float figureHeight = 2.9f;

        [Tooltip("Longest the commentator stays if play does not resume, i.e. after the final goal.")]
        [SerializeField] private float maxHoldSeconds = 6f;

        private const float PopInSeconds = 0.28f;
        private const float PopOutSeconds = 0.18f;

        private GameManager game;
        private Canvas canvas;
        private RectTransform parentRect;
        private Coroutine routine;
        private float sideSign = 1f;
        private bool playResumed;

        private void Awake()
        {
            if (portrait != null && commentators != null && commentators.Length > 0)
                portrait.sprite = commentators[Random.Range(0, commentators.Length)];

            canvas = GetComponentInParent<Canvas>();
            if (figure != null)
            {
                parentRect = figure.parent as RectTransform;
                figure.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            game = GameManager.Instance;
            if (game == null) return;
            game.GoalScored += OnGoalScored;
            game.StateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (game == null) return;
            game.GoalScored -= OnGoalScored;
            game.StateChanged -= OnStateChanged;
        }

        private void OnGoalScored(Side scorer)
        {
            if (figure == null) return;

            // The ball went into the other side's goal, so the commentator stands above it.
            sideSign = scorer == Side.Left ? 1f : -1f;
            playResumed = false;

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Show());
        }

        /// <summary>The kickoff whistle ends the call: the game is back, the commentator is not.</summary>
        private void OnStateChanged(MatchState state)
        {
            if (state != MatchState.Playing || routine == null) return;
            playResumed = true;
            AudioManager.Instance?.StopCommentator();
        }

        /// <summary>
        /// Pop in with a little overshoot, shout, hold until play resumes, pop out.
        /// Unscaled time, because the goal freeze and the Super slow motion both play
        /// with Time.timeScale.
        /// </summary>
        private IEnumerator Show()
        {
            figure.gameObject.SetActive(true);
            AudioManager.Instance?.PlayCommentator();

            for (float t = 0f; t < PopInSeconds; t += Time.unscaledDeltaTime)
            {
                float u = t / PopInSeconds;
                // Ease out with a small overshoot, the way a TV graphic lands.
                Place(1f - Mathf.Pow(1f - u, 3f) + 0.15f * Mathf.Sin(u * Mathf.PI));
                yield return null;
            }

            for (float held = 0f; !playResumed && held < maxHoldSeconds; held += Time.unscaledDeltaTime)
            {
                Place(1f);
                yield return null;
            }

            for (float t = 0f; t < PopOutSeconds; t += Time.unscaledDeltaTime)
            {
                float u = t / PopOutSeconds;
                Place(Mathf.Lerp(1f, 0.01f, u * u));
                yield return null;
            }

            figure.gameObject.SetActive(false);
            routine = null;
        }

        /// <summary>
        /// Puts the figure's feet on the world point above the goal and sizes it so that
        /// <see cref="figureHeight"/> world units fill its reference height, then applies
        /// the pop animation on top.
        /// </summary>
        private void Place(float pop)
        {
            if (parentRect == null) return;

            Camera camera = canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (camera == null) return;

            Vector3 feet = new Vector3(sideSign * goalSideX, standY, 0f);
            Vector3 head = feet + Vector3.up * figureHeight;
            if (!WorldToLocal(camera, feet, out Vector2 feetLocal) || !WorldToLocal(camera, head, out Vector2 headLocal))
                return;

            float referenceHeight = Mathf.Max(1f, figure.rect.height);
            float worldScale = (headLocal.y - feetLocal.y) / referenceHeight;

            figure.anchorMin = figure.anchorMax = new Vector2(0.5f, 0.5f);
            figure.anchoredPosition = feetLocal;
            figure.localScale = Vector3.one * Mathf.Max(0.01f, worldScale * pop);
        }

        private bool WorldToLocal(Camera camera, Vector3 world, out Vector2 local)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            // A screen-space camera canvas needs its camera for the conversion; an overlay canvas needs none.
            Camera eventCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screen, eventCamera, out local);
        }
    }
}
