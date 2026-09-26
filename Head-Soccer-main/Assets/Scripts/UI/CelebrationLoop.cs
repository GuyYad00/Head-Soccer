using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HeadSoccer
{
    /// <summary>
    /// Brings the Character Select portrait to life: the character stands for a moment,
    /// hops into his signature celebration, performs it, and drops back to idle, forever.
    /// Each character has a single celebration frame; the movement that sells it
    /// (kneel, heartbeat, bow, backflip) is done here on the RectTransform, so one
    /// drawing per character is enough. Runs on unscaled time so it also plays while
    /// the menu has Time.timeScale at zero. Match.unity never uses this component.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CelebrationLoop : MonoBehaviour
    {
        [Tooltip("Seconds spent standing before each celebration.")]
        [SerializeField] private float idleHold = 1.1f;
        [Tooltip("How high the character hops when switching pose, in UI pixels.")]
        [SerializeField] private float hopHeight = 40f;

        private Image image;
        private RectTransform rect;
        private Vector2 basePosition;
        private Vector3 baseScale;
        private Coroutine routine;

        private Sprite idle;
        private Sprite celebration;
        private CelebrationStyle style;

        private void Awake() => EnsureInitialised();

        /// <summary>
        /// CharacterSelect.OnEnable may call Play before this component's Awake has run
        /// (Unity does not order Awake across sibling objects), so both paths set up here.
        /// </summary>
        private void EnsureInitialised()
        {
            if (rect != null) return;
            image = GetComponent<Image>();
            rect = (RectTransform)transform;
            basePosition = rect.anchoredPosition;
            baseScale = rect.localScale;
        }

        /// <summary>Starts (or restarts) the loop for a freshly selected character.</summary>
        public void Play(CharacterDefinition character)
        {
            EnsureInitialised();
            Stop();
            idle = character.portrait;
            celebration = character.celebration != null ? character.celebration : character.portrait;
            style = character.celebration != null ? character.celebrationStyle : CelebrationStyle.None;

            image.sprite = idle;
            if (isActiveAndEnabled) StartLoop();
        }

        private void OnEnable() => StartLoop();

        private void OnDisable() => Stop();

        private void StartLoop()
        {
            if (routine != null || idle == null || style == CelebrationStyle.None) return;
            routine = StartCoroutine(Loop());
        }

        private void Stop()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            ResetPose();
        }

        private void ResetPose()
        {
            if (rect == null) return;
            rect.anchoredPosition = basePosition;
            rect.localScale = baseScale;
            rect.localRotation = Quaternion.identity;
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                image.sprite = idle;
                yield return Breathe(idleHold);

                // A small hop separates idle from the celebration and hides the sprite swap.
                yield return Hop(0.28f, hopHeight, swapTo: celebration);

                switch (style)
                {
                    case CelebrationStyle.KissBadge: yield return KissBadge(); break;
                    case CelebrationStyle.Heart: yield return Heartbeat(); break;
                    case CelebrationStyle.Bow: yield return Bow(); break;
                    case CelebrationStyle.Flip: yield return Flip(); break;
                }

                yield return Hop(0.24f, hopHeight * 0.6f, swapTo: idle);
                ResetPose();
            }
        }

        /// <summary>Idle: a barely visible breathing scale so the still frame never looks frozen.</summary>
        private IEnumerator Breathe(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float s = 1f + 0.015f * Mathf.Sin(t * 4.5f);
                rect.localScale = new Vector3(baseScale.x * (2f - s), baseScale.y * s, baseScale.z);
                yield return null;
            }
            rect.localScale = baseScale;
        }

        /// <summary>A parabolic hop; the sprite changes at the top so the swap reads as motion.</summary>
        private IEnumerator Hop(float seconds, float height, Sprite swapTo)
        {
            bool swapped = false;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float u = t / seconds;
                float y = 4f * height * u * (1f - u);
                rect.anchoredPosition = basePosition + new Vector2(0f, y);
                if (!swapped && u >= 0.5f) { image.sprite = swapTo; swapped = true; }
                yield return null;
            }
            image.sprite = swapTo;
            rect.anchoredPosition = basePosition;
        }

        /// <summary>Kneels: sinks a little, then two slow kisses (tiny forward pushes).</summary>
        private IEnumerator KissBadge()
        {
            const float duration = 1.6f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float settle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.25f));
                float kiss = Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 2f / 0.8f));
                rect.anchoredPosition = basePosition + new Vector2(kiss * 6f, -12f * settle);
                rect.localRotation = Quaternion.Euler(0f, 0f, -3f * kiss);
                yield return null;
            }
        }

        /// <summary>Hands in a heart: three heartbeats, a quick double pulse each.</summary>
        private IEnumerator Heartbeat()
        {
            const float duration = 1.7f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float phase = (t % 0.56f) / 0.56f;
                float beat = Mathf.Exp(-phase * 9f) * Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 3f));
                float s = 1f + 0.08f * beat;
                rect.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
                yield return null;
            }
            rect.localScale = baseScale;
        }

        /// <summary>Bows: leans forward from the feet twice, holding each bow for a beat.</summary>
        private IEnumerator Bow()
        {
            const float duration = 1.8f;
            float height = rect.rect.height;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float u = t / duration;
                // Two bows: down, hold, up, and again.
                float wave = Mathf.Clamp01(Mathf.Sin(u * Mathf.PI * 2f) * 1.4f);
                float angle = -22f * Mathf.SmoothStep(0f, 1f, wave);
                rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                // Rotate around the feet rather than the centre: shift so the bottom stays put.
                float rad = angle * Mathf.Deg2Rad;
                rect.anchoredPosition = basePosition + new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad) - 1f) * (height * 0.5f);
                yield return null;
            }
            ResetPose();
        }

        /// <summary>Backflip: one full turn in the air on a high arc, landing back on the feet.</summary>
        private IEnumerator Flip()
        {
            const float duration = 0.95f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float u = t / duration;
                float y = 4f * hopHeight * 2.2f * u * (1f - u);
                rect.anchoredPosition = basePosition + new Vector2(0f, y);
                // The frame is drawn upside down (mid-flip), so start half a turn in.
                rect.localRotation = Quaternion.Euler(0f, 0f, 180f + Mathf.SmoothStep(0f, 1f, u) * 360f);
                yield return null;
            }
            rect.localRotation = Quaternion.Euler(0f, 0f, 180f);
            rect.anchoredPosition = basePosition;
        }
    }
}
