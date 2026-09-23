using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Insets a UI panel to the device safe area, so the scoreboard and the touch
    /// buttons never sit under a notch, a punch hole or the Android gesture bar.
    /// On a desktop screen the safe area equals the full screen and this does nothing.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform panel;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            panel = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea ||
                Screen.width != lastScreenSize.x ||
                Screen.height != lastScreenSize.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0) return;

            // Anchors are normalised, so the safe area in pixels converts directly.
            Vector2 min = new Vector2(lastSafeArea.xMin / Screen.width,
                                      lastSafeArea.yMin / Screen.height);
            Vector2 max = new Vector2(lastSafeArea.xMax / Screen.width,
                                      lastSafeArea.yMax / Screen.height);

            panel.anchorMin = min;
            panel.anchorMax = max;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
        }
    }
}
