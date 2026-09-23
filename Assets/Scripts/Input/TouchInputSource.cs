using UnityEngine;

namespace HeadSoccer
{
    public enum TouchAction { Left, Right, Jump, Kick, Super }

    /// <summary>
    /// Fed by the on-screen buttons of the Android build. Lives on the touch controls
    /// panel in the Canvas; HoldButton components report presses into it.
    /// </summary>
    public class TouchInputSource : MonoBehaviour, IInputSource
    {
        private bool holdLeft, holdRight;
        private bool jumpQueued, kickQueued, superQueued;

        public float Horizontal { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool KickPressedThisFrame { get; private set; }
        public bool SuperPressedThisFrame { get; private set; }

        public void Poll()
        {
            Horizontal = (holdRight ? 1f : 0f) - (holdLeft ? 1f : 0f);

            JumpPressedThisFrame = jumpQueued;
            KickPressedThisFrame = kickQueued;
            SuperPressedThisFrame = superQueued;
            jumpQueued = false;
            kickQueued = false;
            superQueued = false;
        }

        public void Press(TouchAction action)
        {
            switch (action)
            {
                case TouchAction.Left: holdLeft = true; break;
                case TouchAction.Right: holdRight = true; break;
                case TouchAction.Jump: jumpQueued = true; break;
                case TouchAction.Kick: kickQueued = true; break;
                case TouchAction.Super: superQueued = true; break;
            }
        }

        public void Release(TouchAction action)
        {
            switch (action)
            {
                case TouchAction.Left: holdLeft = false; break;
                case TouchAction.Right: holdRight = false; break;
            }
        }

        private void OnDisable()
        {
            holdLeft = holdRight = false;
            jumpQueued = kickQueued = superQueued = false;
        }
    }
}
