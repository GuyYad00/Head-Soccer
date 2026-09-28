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

                if (style == CelebrationStyle.Flip)
                {
                    // The flip is its own take-off and landing.
                    yield return Flip();
                }
                else
                {
                    // A small hop separates idle from the celebration and hides the sprite swap.
                    yield return Hop(0.28f, hopHeight, swapTo: celebration);

                    switch (style)
                    {
                        case CelebrationStyle.KissBadge: yield return KissBadge(); break;
                        case CelebrationStyle.Heart: yield return Heartbeat(); break;
                        case CelebrationStyle.Bow: yield return Bow(); break;
                        case CelebrationStyle.Cheer: yield return Cheer(); break;
                        case CelebrationStyle.KneeSlide: yield return KneeSlide(); break;
                    }

                    yield return Hop(0.24f, hopHeight * 0.6f, swapTo: idle);
                }

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

        /// <summary>
        /// Bows toward the viewer, not sideways. The bow frame faces the camera with the
        /// head down, so the motion is foreshortening: the body shortens from the feet
        /// and widens a little as it leans at us, twice, holding each bow for a beat.
        /// </summary>
        private IEnumerator Bow()
        {
            const float duration = 2.0f;
            float height = rect.rect.height * baseScale.y;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float u = t / duration;
                // Two bows: lean in, hold, straighten, and again.
                float wave = Mathf.Clamp01(Mathf.Sin(u * Mathf.PI * 2f) * 1.4f);
                float lean = Mathf.SmoothStep(0f, 1f, wave);
                float sy = 1f - 0.16f * lean;
                float sx = 1f + 0.05f * lean;
                rect.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
                // Scale happens around the centre; drop the sprite so the feet stay planted.
                rect.anchoredPosition = basePosition + new Vector2(0f, -(1f - sy) * height * 0.5f);
                yield return null;
            }
            ResetPose();
        }

        /// <summary>
        /// Arms up to the sky: joyful little bounces on the spot with a sway from side
        /// to side, like celebrating in front of the crowd.
        /// </summary>
        private IEnumerator Cheer()
        {
            const float duration = 1.8f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                // Ease the bounces in and out so she lands calmly before the hop back.
                float envelope = Mathf.Clamp01(t / 0.2f) * Mathf.Clamp01((duration - t) / 0.3f);
                float bounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2.4f));
                rect.anchoredPosition = basePosition + new Vector2(0f, bounce * hopHeight * 0.5f * envelope);
                rect.localRotation = Quaternion.Euler(0f, 0f, 5f * Mathf.Sin(t * Mathf.PI * 1.2f) * envelope);
                yield return null;
            }
            ResetPose();
        }

        /// <summary>
        /// Knee slide straight at the camera, the same trick as the bow but in
        /// reverse: instead of shrinking away she GROWS, as if sliding out of the
        /// screen toward the viewer. She stays centred on the card, slides in fast,
        /// brakes, holds the pose for the photographers, then eases back into depth.
        /// </summary>
        private IEnumerator KneeSlide()
        {
            const float duration = 1.9f;
            const float zoom = 0.30f;   // how much she grows as she comes at us
            float height = rect.rect.height * baseScale.y;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float u = t / duration;
                float approach;
                if (u < 0.45f)
                {
                    // Fast start that dies out, the way a slide brakes on wet grass.
                    float k = u / 0.45f;
                    approach = 1f - Mathf.Pow(1f - k, 3f);
                }
                else if (u < 0.75f)
                {
                    approach = 1f;   // hold the pose in the viewer's face
                }
                else
                {
                    approach = 1f - Mathf.SmoothStep(0f, 1f, (u - 0.75f) / 0.25f);
                }

                float scale = 1f + zoom * approach;
                rect.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, baseScale.z);
                // Coming closer also means sinking a little in frame, so the knees
                // keep gliding along the same ground line instead of floating up.
                rect.anchoredPosition = basePosition + new Vector2(0f, -height * zoom * approach * 0.45f);
                // A touch of lean while she is moving, straight while she holds.
                float lean = Mathf.Clamp01(u * 5f) * Mathf.Clamp01((0.85f - u) * 4f);
                rect.localRotation = Quaternion.Euler(0f, 0f, -6f * lean);
                yield return null;
            }
            ResetPose();
        }

        /// <summary>
        /// Backflip. The celebration frame is drawn upside down, exactly the mid-air
        /// moment, so it appears when the body is inverted at the top of the arc and the
        /// rotation runs half a turn before and half a turn after it. A crouch on the way
        /// in and a landing squash sell the effort.
        /// </summary>
        private IEnumerator Flip()
        {
            float height = rect.rect.height * baseScale.y;

            // Crouch, still in the idle drawing.
            yield return Squash(0.18f, 0.86f, height);

            const float airTime = 0.9f;
            image.sprite = celebration;
            rect.localScale = baseScale;
            for (float t = 0f; t < airTime; t += Time.unscaledDeltaTime)
            {
                float u = t / airTime;
                float y = 4f * hopHeight * 2.4f * u * (1f - u);
                rect.anchoredPosition = basePosition + new Vector2(0f, y);
                // Backflip for a character facing right: head goes back over the left.
                // 0 degrees is the frame as drawn (inverted), reached at the top of the arc.
                float turn = Mathf.Lerp(-180f, 180f, u);
                rect.localRotation = Quaternion.Euler(0f, 0f, turn);
                yield return null;
            }

            // Land on the feet in the idle drawing, absorb, and stand up.
            image.sprite = idle;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition = basePosition;
            yield return Squash(0.12f, 0.8f, height);
            yield return Unsquash(0.16f, 0.8f, height);
        }

        /// <summary>Scales the body down from the feet over the given time.</summary>
        private IEnumerator Squash(float seconds, float targetY, float height)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float sy = Mathf.Lerp(1f, targetY, Mathf.SmoothStep(0f, 1f, t / seconds));
                ApplySquash(sy, height);
                yield return null;
            }
            ApplySquash(targetY, height);
        }

        /// <summary>Returns the body from a squash to its normal height.</summary>
        private IEnumerator Unsquash(float seconds, float fromY, float height)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float sy = Mathf.Lerp(fromY, 1f, Mathf.SmoothStep(0f, 1f, t / seconds));
                ApplySquash(sy, height);
                yield return null;
            }
            ResetPose();
        }

        private void ApplySquash(float sy, float height)
        {
            float sx = 1f + (1f - sy) * 0.6f;
            rect.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
            rect.anchoredPosition = basePosition + new Vector2(0f, -(1f - sy) * height * 0.5f);
        }
    }
}
