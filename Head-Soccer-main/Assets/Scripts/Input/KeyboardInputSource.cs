using UnityEngine.InputSystem;

namespace HeadSoccer
{
    public enum SuperCombo { TabAndShift, UpAndDown }

    /// <summary>
    /// Reads one key set from the Input System. Super is a two-key chord so it
    /// cannot fire from a single accidental press.
    /// </summary>
    public class KeyboardInputSource : IInputSource
    {
        private readonly Key left, right, jump, kick;
        private readonly SuperCombo superCombo;
        private bool superWasHeld;

        public float Horizontal { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool KickPressedThisFrame { get; private set; }
        public bool SuperPressedThisFrame { get; private set; }

        public KeyboardInputSource(Key left, Key right, Key jump, Key kick, SuperCombo superCombo)
        {
            this.left = left;
            this.right = right;
            this.jump = jump;
            this.kick = kick;
            this.superCombo = superCombo;
        }

        /// <summary>Player one reads the layout chosen on the menu (WASD unless swapped).</summary>
        public static KeyboardInputSource PlayerOne() => For(MatchSettings.PlayerOneLayout);

        /// <summary>Player two always takes the layout player one is not using.</summary>
        public static KeyboardInputSource PlayerTwo() => For(MatchSettings.PlayerTwoLayout);

        public static KeyboardInputSource For(KeyboardLayout layout) =>
            layout == KeyboardLayout.Arrows ? Arrows() : Wasd();

        public static KeyboardInputSource Wasd() =>
            new KeyboardInputSource(Key.A, Key.D, Key.W, Key.Space, SuperCombo.TabAndShift);

        public static KeyboardInputSource Arrows() =>
            new KeyboardInputSource(Key.LeftArrow, Key.RightArrow, Key.UpArrow, Key.RightCtrl, SuperCombo.UpAndDown);

        /// <summary>Short label for the menu and the controls hint.</summary>
        public static string Describe(KeyboardLayout layout) =>
            layout == KeyboardLayout.Arrows
                ? "ARROWS move    UP jump    RIGHT CTRL kick"
                : "A / D move    W jump    SPACE kick";

        public void Poll()
        {
            Horizontal = 0f;
            JumpPressedThisFrame = false;
            KickPressedThisFrame = false;
            SuperPressedThisFrame = false;

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb[left].isPressed) Horizontal -= 1f;
            if (kb[right].isPressed) Horizontal += 1f;
            JumpPressedThisFrame |= kb[jump].wasPressedThisFrame;
            KickPressedThisFrame |= kb[kick].wasPressedThisFrame;

            bool comboHeld = superCombo == SuperCombo.TabAndShift
                ? kb[Key.Tab].isPressed && (kb[Key.LeftShift].isPressed || kb[Key.RightShift].isPressed)
                : kb[Key.UpArrow].isPressed && kb[Key.DownArrow].isPressed;

            SuperPressedThisFrame = comboHeld && !superWasHeld;
            superWasHeld = comboHeld;
        }
    }
}
