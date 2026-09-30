using NUnit.Framework;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// The victory anthem is a reward, not a jingle. It plays for one result only.
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
    }
}
