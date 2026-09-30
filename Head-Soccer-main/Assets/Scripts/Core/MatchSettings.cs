using UnityEngine;

namespace HeadSoccer
{
    public enum GameMode { OnePlayerVsCPU, TwoPlayers }

    /// <summary>
    /// The two keyboard layouts. Player one gets one of them and player two the other,
    /// so the pair never collides on the same keys.
    /// </summary>
    public enum KeyboardLayout { Wasd, Arrows }

    /// <summary>
    /// The few choices that have to survive the trip from Menu.unity to Match.unity.
    /// Backed by PlayerPrefs, which the GDD names as the only save system in scope.
    /// </summary>
    public static class MatchSettings
    {
        private const string ModeKey = "hs_game_mode_v2";
        private const string DifficultyKey = "hs_ai_difficulty";
        private const string LayoutKey = "hs_p1_keys";

        /// <summary>
        /// Which keys player one uses. WASD + Space is the default; the menu button
        /// swaps it for the arrows + Right Ctrl, and player two takes whichever is left.
        /// </summary>
        public static KeyboardLayout PlayerOneLayout
        {
            get => (KeyboardLayout)PlayerPrefs.GetInt(LayoutKey, (int)KeyboardLayout.Wasd);
            set { PlayerPrefs.SetInt(LayoutKey, (int)value); PlayerPrefs.Save(); }
        }

        public static KeyboardLayout PlayerTwoLayout =>
            PlayerOneLayout == KeyboardLayout.Wasd ? KeyboardLayout.Arrows : KeyboardLayout.Wasd;

        public static GameMode Mode
        {
            get => (GameMode)PlayerPrefs.GetInt(ModeKey, (int)GameMode.TwoPlayers);
            set { PlayerPrefs.SetInt(ModeKey, (int)value); PlayerPrefs.Save(); }
        }

        public static AIDifficulty Difficulty
        {
            get => (AIDifficulty)PlayerPrefs.GetInt(DifficultyKey, (int)AIDifficulty.Medium);
            set { PlayerPrefs.SetInt(DifficultyKey, (int)value); PlayerPrefs.Save(); }
        }

        /// <summary>True on a real phone, or in the editor when a touch screen is simulated.</summary>
        public static bool UseTouchControls => Application.isMobilePlatform;

        /// <summary>
        /// The victory anthem is only for the human beating the CPU. Player one is always
        /// the left side (winner 0). A two-player win, a loss to the CPU and a draw are not it.
        /// </summary>
        public static bool HumanBeatTheCpu(GameMode mode, int winnerIndex) =>
            mode == GameMode.OnePlayerVsCPU && winnerIndex == 0;
    }
}
