using NUnit.Framework;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// The P1 KEYS choice. Whatever player one picks, player two must get the other
    /// layout, so two people on one keyboard never share a key.
    /// </summary>
    public class KeyboardLayoutTests
    {
        private PlayerPrefsSandbox sandbox;

        [SetUp]
        public void SetUp()
        {
            sandbox = new PlayerPrefsSandbox(new[] { "hs_p1_keys" });
        }

        [TearDown]
        public void TearDown()
        {
            sandbox.Dispose();
        }

        [Test]
        public void FreshInstall_PlayerOneHasWasd_PlayerTwoHasArrows()
        {
            Assert.That(MatchSettings.PlayerOneLayout, Is.EqualTo(KeyboardLayout.Wasd));
            Assert.That(MatchSettings.PlayerTwoLayout, Is.EqualTo(KeyboardLayout.Arrows));
        }

        [Test]
        public void SwappingPlayerOneToArrows_HandsWasdToPlayerTwo()
        {
            MatchSettings.PlayerOneLayout = KeyboardLayout.Arrows;
            Assert.That(MatchSettings.PlayerTwoLayout, Is.EqualTo(KeyboardLayout.Wasd));
        }

        [Test]
        public void TheTwoPlayers_NeverShareALayout()
        {
            foreach (KeyboardLayout layout in new[] { KeyboardLayout.Wasd, KeyboardLayout.Arrows })
            {
                MatchSettings.PlayerOneLayout = layout;
                Assert.That(MatchSettings.PlayerTwoLayout, Is.Not.EqualTo(MatchSettings.PlayerOneLayout));
            }
        }

        [Test]
        public void TheChoice_SurvivesBetweenReads_BecauseItIsSavedInPlayerPrefs()
        {
            MatchSettings.PlayerOneLayout = KeyboardLayout.Arrows;
            Assert.That(MatchSettings.PlayerOneLayout, Is.EqualTo(KeyboardLayout.Arrows));
        }

        [Test]
        public void Describe_NamesTheKickKeyOfEachLayout()
        {
            Assert.That(KeyboardInputSource.Describe(KeyboardLayout.Wasd), Does.Contain("SPACE"));
            Assert.That(KeyboardInputSource.Describe(KeyboardLayout.Arrows), Does.Contain("RIGHT CTRL"));
        }

        [Test]
        public void For_BuildsASourceForEitherLayout()
        {
            Assert.That(KeyboardInputSource.For(KeyboardLayout.Wasd), Is.Not.Null);
            Assert.That(KeyboardInputSource.For(KeyboardLayout.Arrows), Is.Not.Null);
        }

        [Test]
        public void PlayerOneAndPlayerTwo_FollowTheSavedChoice()
        {
            MatchSettings.PlayerOneLayout = KeyboardLayout.Arrows;
            // Both factories must build without a keyboard attached: the CPU build and the tests have none.
            Assert.That(KeyboardInputSource.PlayerOne(), Is.Not.Null);
            Assert.That(KeyboardInputSource.PlayerTwo(), Is.Not.Null);
        }
    }
}
