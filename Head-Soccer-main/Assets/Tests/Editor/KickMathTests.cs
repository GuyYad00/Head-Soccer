using NUnit.Framework;
using UnityEngine;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// The numbers behind a kick. The GDD promises two things: a running kick hits up
    /// to kickMomentumBonus harder than a standing one and never weaker, and every kick
    /// leaves the foot between kickMinAngle and kickMaxAngle in the attacking direction.
    /// </summary>
    public class KickMathTests
    {
        private const float MoveSpeed = 6f;
        private const float Bonus = 0.5f;

        [Test]
        public void MomentumFactor_StandingStill_IsExactlyOne()
        {
            Assert.That(KickMath.MomentumFactor(0f, MoveSpeed, Bonus), Is.EqualTo(1f));
        }

        [Test]
        public void MomentumFactor_FullSpeedTowardGoal_AddsTheWholeBonus()
        {
            Assert.That(KickMath.MomentumFactor(MoveSpeed, MoveSpeed, Bonus), Is.EqualTo(1f + Bonus).Within(1e-5f));
        }

        [Test]
        public void MomentumFactor_HalfSpeed_AddsHalfTheBonus()
        {
            Assert.That(KickMath.MomentumFactor(MoveSpeed * 0.5f, MoveSpeed, Bonus), Is.EqualTo(1f + Bonus * 0.5f).Within(1e-5f));
        }

        [Test]
        public void MomentumFactor_RunningAwayFromTheBall_NeverWeakensTheKick()
        {
            Assert.That(KickMath.MomentumFactor(-MoveSpeed, MoveSpeed, Bonus), Is.EqualTo(1f));
        }

        [Test]
        public void MomentumFactor_FasterThanFullSpeed_IsCappedAtTheBonus()
        {
            // A shove or a Super can push a player past moveSpeed; the kick must not scale past the cap.
            Assert.That(KickMath.MomentumFactor(MoveSpeed * 3f, MoveSpeed, Bonus), Is.EqualTo(1f + Bonus).Within(1e-5f));
        }

        [Test]
        public void MomentumFactor_ZeroMoveSpeedInConfig_DoesNotDivideByZero()
        {
            Assert.That(KickMath.MomentumFactor(4f, 0f, Bonus), Is.EqualTo(1f));
        }

        [Test]
        public void Direction_ZeroDegreesFacingRight_IsAFlatDriveToTheRight()
        {
            Vector2 direction = KickMath.Direction(0f, 1f);
            Assert.That(direction.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(direction.y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Direction_FacingLeft_MirrorsOnlyTheHorizontal()
        {
            Vector2 right = KickMath.Direction(30f, 1f);
            Vector2 left = KickMath.Direction(30f, -1f);
            Assert.That(left.x, Is.EqualTo(-right.x).Within(1e-5f));
            Assert.That(left.y, Is.EqualTo(right.y).Within(1e-5f));
        }

        [Test]
        public void Direction_NinetyDegrees_IsStraightUp()
        {
            Vector2 direction = KickMath.Direction(90f, 1f);
            Assert.That(direction.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(direction.y, Is.EqualTo(1f).Within(1e-5f));
        }

        [TestCase(0f)]
        [TestCase(15f)]
        [TestCase(45f)]
        [TestCase(70f)]
        public void Direction_IsAlwaysAUnitVector_SoTheImpulseOnlyDependsOnPower(float angle)
        {
            Assert.That(KickMath.Direction(angle, 1f).magnitude, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Direction_InsideTheConfiguredRange_AlwaysSendsTheBallForwardAndNotDown()
        {
            for (float angle = 0f; angle <= 45f; angle += 5f)
            {
                Vector2 direction = KickMath.Direction(angle, 1f);
                Assert.That(direction.x, Is.GreaterThan(0f), $"angle {angle} should travel forward");
                Assert.That(direction.y, Is.GreaterThanOrEqualTo(0f), $"angle {angle} should never point into the grass");
            }
        }
    }
}
