using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Keeps the whole pitch on screen on any aspect ratio, which the single screen
    /// design pillar requires. A fixed orthographic size only frames the pitch
    /// correctly at one aspect: on a narrow screen such as 4:3 the goals fall off the
    /// sides, so the camera zooms out until the full width fits.
    /// Wider phones simply reveal more of the stadium background.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        [Tooltip("Half the width of the play area that must always be visible, in world units.")]
        [SerializeField] private float requiredHalfWidth = 9.2f;

        [Tooltip("Vertical framing on a wide screen. The camera never zooms in past this.")]
        [SerializeField] private float minOrthographicSize = 5f;

        private Camera view;
        private float lastAspect;

        private void Awake()
        {
            view = GetComponent<Camera>();
            Fit();
        }

        private void Update()
        {
            // Cheap guard so this does nothing on the frames where nothing changed,
            // but still reacts to a device rotation or a resized editor Game view.
            if (!Mathf.Approximately(view.aspect, lastAspect)) Fit();
        }

        private void Fit()
        {
            lastAspect = view.aspect;
            float sizeNeededForWidth = requiredHalfWidth / Mathf.Max(0.1f, view.aspect);
            view.orthographicSize = Mathf.Max(minOrthographicSize, sizeNeededForWidth);
        }
    }
}
