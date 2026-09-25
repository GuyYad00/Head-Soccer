using UnityEngine;

namespace HeadSoccer
{
    public enum GameMode { OnePlayerVsCPU, TwoPlayers }

    /// <summary>
    /// The few choices that have to survive the trip from Menu.unity to Match.unity.
    /// Backed by PlayerPrefs, which the GDD names as the only save system in scope.
    /// </summary>
    public static class MatchSettings
    {
        private const string ModeKey = "hs_game_mode_v2";
        private const string DifficultyKey = "hs_ai_difficulty";

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
    }
}
