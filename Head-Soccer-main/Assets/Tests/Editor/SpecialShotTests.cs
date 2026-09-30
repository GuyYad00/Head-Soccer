using NUnit.Framework;
using UnityEngine;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// The once-per-match Super. It fills only from ball contact, fires exactly once,
    /// and stays dead for the rest of the match until a rematch resets it.
    /// </summary>
    public class SpecialShotTests
    {
        private GameConfig config;
        private SpecialShot shot;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            config.superChargeTime = 6f;
            config.superKickCharge = 0.12f;
            config.superBounceCharge = 0.08f;
            shot = new SpecialShot(config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void StartsEmpty_NotReady_NotUsed()
        {
            Assert.That(shot.Charge, Is.EqualTo(0f));
            Assert.That(shot.IsReady, Is.False);
            Assert.That(shot.IsUsed, Is.False);
        }

        [Test]
        public void ContactForHalfTheChargeTime_FillsHalfTheMeter()
        {
            shot.ChargeFromContact(3f);
            Assert.That(shot.Charge, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(shot.IsReady, Is.False);
        }

        [Test]
        public void ContactForTheFullChargeTime_MakesTheShotReady()
        {
            shot.ChargeFromContact(6f);
            Assert.That(shot.IsReady, Is.True);
        }

        [Test]
        public void KickAndBounce_AddTheirConfiguredSlices()
        {
            shot.ChargeFromKick();
            shot.ChargeFromBounce();
            Assert.That(shot.Charge, Is.EqualTo(0.20f).Within(1e-4f));
        }

        [Test]
        public void Meter_NeverOverfills()
        {
            shot.ChargeFromContact(60f);
            for (int i = 0; i < 20; i++) shot.ChargeFromKick();
            Assert.That(shot.Charge, Is.EqualTo(1f));
        }

        [Test]
        public void TryFire_BeforeReady_DoesNothing()
        {
            shot.ChargeFromContact(2f);
            Assert.That(shot.TryFire(), Is.False);
            Assert.That(shot.IsUsed, Is.False);
            Assert.That(shot.Charge, Is.EqualTo(2f / 6f).Within(1e-4f));
        }

        [Test]
        public void TryFire_WhenReady_SpendsTheMeterExactlyOnce()
        {
            shot.ChargeFromContact(6f);
            Assert.That(shot.TryFire(), Is.True);
            Assert.That(shot.IsUsed, Is.True);
            Assert.That(shot.Charge, Is.EqualTo(0f));
            Assert.That(shot.TryFire(), Is.False, "the Super is once per match");
        }

        [Test]
        public void AfterFiring_NoContactCanRechargeIt()
        {
            shot.ChargeFromContact(6f);
            shot.TryFire();
            shot.ChargeFromContact(60f);
            shot.ChargeFromKick();
            Assert.That(shot.Charge, Is.EqualTo(0f));
            Assert.That(shot.IsReady, Is.False);
        }

        [Test]
        public void Reset_GivesANewMatchAFreshSuper()
        {
            shot.ChargeFromContact(6f);
            shot.TryFire();
            shot.Reset();
            Assert.That(shot.IsUsed, Is.False);
            shot.ChargeFromContact(6f);
            Assert.That(shot.IsReady, Is.True);
        }

        [Test]
        public void AbsurdlyShortChargeTimeInConfig_IsClampedSoOneFrameCannotFillIt()
        {
            config.superChargeTime = 0f;
            shot.ChargeFromContact(0.02f);
            Assert.That(shot.IsReady, Is.False);
        }
    }
}
