using NUnit.Framework;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// The two CPU songs are rewards for a result, not jingles. Each plays for one result only.
    /// </summary>
    public class HumanVictoryTests
    {
        [Test]
        public void Anthem_PlaysWhenTheHumanBeatsTheCpu()
        {
            Assert.That(MatchSettings.HumanBeatTheCpu(GameMode.OnePlayerVsCPU, winnerIndex: 0), Is.True);
        }

        [TestCase(GameMode.OnePlayerVsCPU, 1)]
        [TestCase(GameMode.OnePlayerVsCPU, -1)]
        [TestCase(GameMode.TwoPlayers, 0)]
        [TestCase(GameMode.TwoPlayers, 1)]
        [TestCase(GameMode.TwoPlayers, -1)]
        public void Anthem_StaysSilentForEveryOtherResult(GameMode mode, int winnerIndex)
        {
            Assert.That(MatchSettings.HumanBeatTheCpu(mode, winnerIndex), Is.False);
        }

        [Test]
        public void Defeat_PlaysWhenTheHumanLosesToTheCpu()
        {
            Assert.That(MatchSettings.HumanLostToTheCpu(GameMode.OnePlayerVsCPU, winnerIndex: 1), Is.True);
        }

        [TestCase(GameMode.OnePlayerVsCPU, 0)]
        [TestCase(GameMode.OnePlayerVsCPU, -1)]
        [TestCase(GameMode.TwoPlayers, 0)]
        [TestCase(GameMode.TwoPlayers, 1)]
        [TestCase(GameMode.TwoPlayers, -1)]
        public void Defeat_StaysSilentForEveryOtherResult(GameMode mode, int winnerIndex)
        {
            Assert.That(MatchSettings.HumanLostToTheCpu(mode, winnerIndex), Is.False);
        }
    }

    public class ResultBannerTests
    {
        [Test]
        public void Banner_TellsTheHumanHeLostToTheCpu()
        {
            Assert.That(UIManager.Banner(GameMode.OnePlayerVsCPU, 1, "YOSSI", "NOA"), Is.EqualTo("YOU LOST"));
            Assert.That(UIManager.Kicker(GameMode.OnePlayerVsCPU, 1), Is.EqualTo("GAME OVER"));
        }

        [Test]
        public void Banner_NamesTheWinnerEverywhereElse()
        {
            Assert.That(UIManager.Banner(GameMode.OnePlayerVsCPU, 0, "YOSSI", "NOA"), Is.EqualTo("YOSSI WINS!"));
            Assert.That(UIManager.Banner(GameMode.TwoPlayers, 1, "YOSSI", "NOA"), Is.EqualTo("NOA WINS!"));
            Assert.That(UIManager.Banner(GameMode.TwoPlayers, -1, "YOSSI", "NOA"), Is.EqualTo("DRAW"));
            Assert.That(UIManager.Kicker(GameMode.TwoPlayers, 1), Is.EqualTo("FULL TIME"));
        }
    }
}
