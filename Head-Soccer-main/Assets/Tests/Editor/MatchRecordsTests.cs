using NUnit.Framework;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// The records on the match over screen: biggest win by goal difference and how
    /// many matches player one has won. A draw records nothing.
    /// </summary>
    public class MatchRecordsTests
    {
        private PlayerPrefsSandbox sandbox;

        [SetUp]
        public void SetUp()
        {
            sandbox = new PlayerPrefsSandbox(new[] { "hs_best_margin", "hs_p1_wins" }, new[] { "hs_best_scoreline" });
        }

        [TearDown]
        public void TearDown()
        {
            sandbox.Dispose();
        }

        [Test]
        public void FreshInstall_HasNoRecords()
        {
            Assert.That(MatchRecords.BestWinMargin, Is.EqualTo(0));
            Assert.That(MatchRecords.PlayerOneWins, Is.EqualTo(0));
            Assert.That(MatchRecords.Summary(), Does.Contain("none yet"));
        }

        [Test]
        public void PlayerOneWin_CountsAWinAndSetsTheBestScoreline()
        {
            MatchRecords.RecordMatch(5, 2, winnerIndex: 0);
            Assert.That(MatchRecords.PlayerOneWins, Is.EqualTo(1));
            Assert.That(MatchRecords.BestWinMargin, Is.EqualTo(3));
            Assert.That(MatchRecords.BestScoreline, Is.EqualTo("5 - 2"));
        }

        [Test]
        public void ASmallerWinLater_KeepsTheBiggerRecord()
        {
            MatchRecords.RecordMatch(5, 2, winnerIndex: 0);
            MatchRecords.RecordMatch(3, 2, winnerIndex: 0);
            Assert.That(MatchRecords.BestWinMargin, Is.EqualTo(3));
            Assert.That(MatchRecords.BestScoreline, Is.EqualTo("5 - 2"));
            Assert.That(MatchRecords.PlayerOneWins, Is.EqualTo(2));
        }

        [Test]
        public void PlayerTwoWin_CanBeTheBestWin_ButDoesNotCountForPlayerOne()
        {
            MatchRecords.RecordMatch(5, 2, winnerIndex: 0);
            MatchRecords.RecordMatch(1, 5, winnerIndex: 1);
            Assert.That(MatchRecords.BestWinMargin, Is.EqualTo(4));
            Assert.That(MatchRecords.BestScoreline, Is.EqualTo("1 - 5"));
            Assert.That(MatchRecords.PlayerOneWins, Is.EqualTo(1));
        }

        [Test]
        public void ADraw_RecordsNothing()
        {
            MatchRecords.RecordMatch(2, 2, winnerIndex: -1);
            Assert.That(MatchRecords.BestWinMargin, Is.EqualTo(0));
            Assert.That(MatchRecords.PlayerOneWins, Is.EqualTo(0));
            Assert.That(MatchRecords.Summary(), Does.Contain("none yet"));
        }

        [Test]
        public void Summary_ShowsTheScorelineTheMarginAndTheWinCount()
        {
            MatchRecords.RecordMatch(5, 1, winnerIndex: 0);
            string summary = MatchRecords.Summary();
            Assert.That(summary, Does.Contain("5 - 1"));
            Assert.That(summary, Does.Contain("by 4"));
            Assert.That(summary, Does.Contain("P1 wins: 1"));
        }
    }
}
