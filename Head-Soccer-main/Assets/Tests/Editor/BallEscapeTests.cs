using NUnit.Framework;
using UnityEngine;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// A ball wedged in a goal gets one push back onto the pitch after two seconds.
    /// Both goals share the rule: the sign of the ball's x picks the side.
    /// </summary>
    public class BallEscapeTests
    {
        private const float GoalLine = 7.7f;
        private const float Wait = 2f;
        private const float StuckSpeed = 0.5f;
        private const float Push = 8f;

        [Test]
        public void RightGoal_AfterTwoSeconds_PushesLeft()
        {
            Vector2 push = BallController.GoalEscape(8.6f, 0f, Wait, GoalLine, Wait, StuckSpeed, Push);

            Assert.That(push.x, Is.EqualTo(-Push));
            Assert.That(push.y, Is.EqualTo(0f));
        }

        [Test]
        public void LeftGoal_AfterTwoSeconds_PushesRight()
        {
            Vector2 push = BallController.GoalEscape(-8.6f, 0f, Wait, GoalLine, Wait, StuckSpeed, Push);

            Assert.That(push.x, Is.EqualTo(Push));
        }

        [Test]
        public void BothGoals_AreTheSamePushMirrored()
        {
            Vector2 right = BallController.GoalEscape(8.6f, 0f, Wait, GoalLine, Wait, StuckSpeed, Push);
            Vector2 left = BallController.GoalEscape(-8.6f, 0f, Wait, GoalLine, Wait, StuckSpeed, Push);

            Assert.That(left.x, Is.EqualTo(-right.x));
        }

        [Test]
        public void BeforeTwoSeconds_DoesNothing()
        {
            Vector2 push = BallController.GoalEscape(8.6f, 0f, Wait - 0.1f, GoalLine, Wait, StuckSpeed, Push);

            Assert.That(push, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void OnThePitch_DoesNothing()
        {
            Vector2 push = BallController.GoalEscape(GoalLine - 0.1f, 0f, 5f, GoalLine, Wait, StuckSpeed, Push);

            Assert.That(push, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void StillMoving_DoesNothing()
        {
            Vector2 push = BallController.GoalEscape(8.6f, StuckSpeed, 5f, GoalLine, Wait, StuckSpeed, Push);

            Assert.That(push, Is.EqualTo(Vector2.zero));
        }
    }
}
