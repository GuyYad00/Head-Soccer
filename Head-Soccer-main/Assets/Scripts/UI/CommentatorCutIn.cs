using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HeadSoccer
{
    /// <summary>
    /// The broadcast cut-in. There is no football without a commentator, so one of
    /// the three is drawn at random when the match starts and stays for the whole
    /// match. On every goal his box pops up over the crowd on the scorer's side, he
    /// shouts the goal call, holds through the celebration freeze and the kickoff
    /// count, and pops away. Listens to GameManager.GoalScored, so the manager never
    /// knows this exists; REMATCH reloads the scene and draws a new commentator.
    /// </summary>
    public class CommentatorCutIn : MonoBehaviour
    {
        [Tooltip("The commentator drawings. One is picked at random per match.")]
        [SerializeField] private Sprite[] commentators;
        [Tooltip("The box that pops in and out; anchored to the top left or top right per goal.")]
        [SerializeField] private RectTransform frame;
        [SerializeField] private Image portrait;
        [Tooltip("Distance from the screen edge to the centre of the box, in reference pixels.")]
        [SerializeField] private float edgeOffset = 175f;
        [Tooltip("Distance from the top of the safe area to the centre of the box.")]
        [SerializeField] private float topOffset = 150f;
        [Tooltip("Seconds the commentator stays on screen after the pop-in.")]
        [SerializeField] private float holdSeconds = 4.2f;

        private const float PopInSeconds = 0.28f;
        private const float PopOutSeconds = 0.18f;

        private GameManager game;
        private Coroutine routine;

        private void Awake()
        {
            if (portrait != null && commentators != null && commentators.Length > 0)
                portrait.sprite = commentators[Random.Range(0, commentators.Length)];

            if (frame != null) frame.gameObject.SetActive(false);
        }

        private void Start()
        {
            game = GameManager.Instance;
            if (game != null) game.GoalScored += OnGoalScored;
        }

        private void OnDestroy()
        {
            if (game != null) game.GoalScored -= OnGoalScored;
        }

        private void OnGoalScored(Side scorer)
        {
            if (frame == null) return;

            // The scorer's own half is the quiet one; the confetti and the flash are on
            // the other side, so the box never fights them for attention.
            float x = scorer == Side.Left ? 0f : 1f;
            frame.anchorMin = frame.anchorMax = new Vector2(x, 1f);
            frame.anchoredPosition = new Vector2(scorer == Side.Left ? edgeOffset : -edgeOffset, -topOffset);

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Show());
        }

        /// <summary>
        /// Pop in with a little overshoot, shout, hold, pop out. Unscaled time, because
        /// the goal freeze and the Super slow motion both play with Time.timeScale.
        /// </summary>
        private IEnumerator Show()
        {
            frame.gameObject.SetActive(true);
            AudioManager.Instance?.PlayCommentator();

            for (float t = 0f; t < PopInSeconds; t += Time.unscaledDeltaTime)
            {
                float u = t / PopInSeconds;
                // Ease out with a small overshoot, the way a TV graphic lands.
                float s = 1f - Mathf.Pow(1f - u, 3f) + 0.15f * Mathf.Sin(u * Mathf.PI);
                frame.localScale = Vector3.one * Mathf.Max(0.01f, s);
                yield return null;
            }
            frame.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(holdSeconds);

            for (float t = 0f; t < PopOutSeconds; t += Time.unscaledDeltaTime)
            {
                float u = t / PopOutSeconds;
                frame.localScale = Vector3.one * Mathf.Lerp(1f, 0.01f, u * u);
                yield return null;
            }

            frame.localScale = Vector3.one;
            frame.gameObject.SetActive(false);
            routine = null;
        }
    }
}
