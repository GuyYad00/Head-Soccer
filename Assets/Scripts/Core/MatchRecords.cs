using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Best results kept in PlayerPrefs, as the GDD allows for settings and records.
    /// Biggest win is the largest goal difference in a finished match.
    /// </summary>
    public static class MatchRecords
    {
        private const string MarginKey = "hs_best_margin";
        private const string ScoreKey = "hs_best_scoreline";
        private const string WinsKey = "hs_p1_wins";

        public static int BestWinMargin => PlayerPrefs.GetInt(MarginKey, 0);
        public static string BestScoreline => PlayerPrefs.GetString(ScoreKey, "none yet");
        public static int PlayerOneWins => PlayerPrefs.GetInt(WinsKey, 0);

        public static void RecordMatch(int left, int right, int winnerIndex)
        {
            if (winnerIndex == 0)
                PlayerPrefs.SetInt(WinsKey, PlayerOneWins + 1);

            int margin = Mathf.Abs(left - right);
            if (winnerIndex >= 0 && margin >= BestWinMargin)
            {
                PlayerPrefs.SetInt(MarginKey, margin);
                PlayerPrefs.SetString(ScoreKey, $"{left} - {right}");
            }

            PlayerPrefs.Save();
        }

        public static string Summary()
        {
            if (BestWinMargin <= 0)
                return "Best win: none yet\nP1 wins: 0";

            return $"Best win: {BestScoreline}  (by {BestWinMargin})\nP1 wins: {PlayerOneWins}";
        }
    }
}
