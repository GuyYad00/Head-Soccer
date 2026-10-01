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
}
