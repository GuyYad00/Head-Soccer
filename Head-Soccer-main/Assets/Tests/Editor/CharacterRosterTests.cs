using NUnit.Framework;
using UnityEngine;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// Character Select arithmetic and the two saved choices. The arrows wrap at both
    /// ends, and a saved index that no longer exists (a character removed from the
    /// roster) must fall back to a valid one instead of crashing the menu.
    /// </summary>
    public class CharacterRosterTests
    {
        private PlayerPrefsSandbox sandbox;

        [SetUp]
        public void SetUp()
        {
            sandbox = new PlayerPrefsSandbox(new[] { "hs_character_p1", "hs_character_p2" });
        }

        [TearDown]
        public void TearDown()
        {
            sandbox.Dispose();
        }

        [TestCase(0, 1, 6, ExpectedResult = 1)]
        [TestCase(5, 1, 6, ExpectedResult = 0, TestName = "Wrap_NextFromTheLastCharacter_GoesBackToTheFirst")]
        [TestCase(0, -1, 6, ExpectedResult = 5, TestName = "Wrap_PreviousFromTheFirstCharacter_GoesToTheLast")]
        [TestCase(3, -1, 6, ExpectedResult = 2)]
        [TestCase(0, 1, 1, ExpectedResult = 0, TestName = "Wrap_WithASingleCharacter_StaysPut")]
        [TestCase(2, 1, 0, ExpectedResult = 0, TestName = "Wrap_WithAnEmptyRoster_IsZeroNotAnException")]
        public int Wrap(int index, int step, int count) => CharacterRoster.Wrap(index, step, count);

        [Test]
        public void FreshInstall_PlayerOneIsTheFirstCharacter_AndTheRightSideIsTheSecond()
        {
            // Never a mirror match on the first run.
            Assert.That(CharacterRoster.SelectedIndex, Is.EqualTo(0));
            Assert.That(CharacterRoster.OpponentIndex, Is.EqualTo(1));
            Assert.That(CharacterRoster.SelectedIndex, Is.Not.EqualTo(CharacterRoster.OpponentIndex));
        }

        [Test]
        public void BothChoices_AreSavedIndependently()
        {
            CharacterRoster.SelectedIndex = 4;
            CharacterRoster.OpponentIndex = 2;
            Assert.That(CharacterRoster.SelectedIndex, Is.EqualTo(4));
            Assert.That(CharacterRoster.OpponentIndex, Is.EqualTo(2));
        }

        [Test]
        public void Get_OnAnEmptyRoster_ReturnsADefaultInsteadOfThrowing()
        {
            var roster = ScriptableObject.CreateInstance<CharacterRoster>();
            try
            {
                Assert.That(roster.Count, Is.EqualTo(0));
                Assert.DoesNotThrow(() => roster.Get(3));
                Assert.That(roster.Get(3).portrait, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(roster);
            }
        }
    }
}
