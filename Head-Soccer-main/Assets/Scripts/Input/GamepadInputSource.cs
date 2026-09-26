using UnityEngine;
using UnityEngine.InputSystem;

namespace HeadSoccer
{
    /// <summary>
    /// Reads one connected gamepad. The first pad drives player one, the second pad
    /// player two, so two controllers on one PC give a proper couch match.
    /// South = jump, West/East = kick, North or a shoulder button = Super.
    /// </summary>
    public class GamepadInputSource : IInputSource
    {
        private const float Deadzone = 0.25f;

        private readonly int padIndex;

        public float Horizontal { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool KickPressedThisFrame { get; private set; }
        public bool SuperPressedThisFrame { get; private set; }

        public GamepadInputSource(int padIndex)
        {
            this.padIndex = padIndex;
        }

        public void Poll()
        {
            Horizontal = 0f;
            JumpPressedThisFrame = false;
            KickPressedThisFrame = false;
            SuperPressedThisFrame = false;

            if (padIndex >= Gamepad.all.Count) return;
            Gamepad pad = Gamepad.all[padIndex];

            float stick = pad.leftStick.x.ReadValue();
            if (pad.dpad.left.isPressed) stick = -1f;
            if (pad.dpad.right.isPressed) stick = 1f;
            Horizontal = Mathf.Abs(stick) < Deadzone ? 0f : Mathf.Sign(stick);

            JumpPressedThisFrame = pad.buttonSouth.wasPressedThisFrame;
            KickPressedThisFrame = pad.buttonWest.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame;
            SuperPressedThisFrame = pad.buttonNorth.wasPressedThisFrame
                                    || pad.rightShoulder.wasPressedThisFrame
                                    || pad.leftShoulder.wasPressedThisFrame;
        }
    }
}
