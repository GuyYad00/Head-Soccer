using UnityEngine;

namespace HeadSoccer
{
    /// <summary>Removed from the match HUD. Kept so old scenes do not break.</summary>
    public class ControlsHint : MonoBehaviour
    {
        private void Awake() => Destroy(gameObject);
    }
}
