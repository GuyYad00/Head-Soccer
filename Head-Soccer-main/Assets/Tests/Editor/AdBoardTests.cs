using NUnit.Framework;
using UnityEngine;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// The advertising board draws one banner strip per match. With two strips that
    /// is a coin toss: every roll below one half is the first strip, the rest is the
    /// second, so both get the same share of matches.
    /// </summary>
    public class AdBoardTests
    {
        private static Sprite Blank(string name)
        {
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            sprite.name = name;
            return sprite;
        }

        [Test]
        public void Draw_TwoBanners_LowerHalfOfTheRollIsTheFirst()
        {
            var banners = new[] { Blank("a"), Blank("b") };

            Assert.That(AdBoard.Draw(banners, 0f), Is.SameAs(banners[0]));
            Assert.That(AdBoard.Draw(banners, 0.49f), Is.SameAs(banners[0]));
        }

        [Test]
        public void Draw_TwoBanners_UpperHalfOfTheRollIsTheSecond()
        {
            var banners = new[] { Blank("a"), Blank("b") };

            Assert.That(AdBoard.Draw(banners, 0.5f), Is.SameAs(banners[1]));
            Assert.That(AdBoard.Draw(banners, 0.99f), Is.SameAs(banners[1]));
        }

        [Test]
        public void Draw_ARollOfExactlyOne_StillPicksTheLastBanner()
        {
            var banners = new[] { Blank("a"), Blank("b") };

            Assert.That(AdBoard.Draw(banners, 1f), Is.SameAs(banners[1]));
        }

        [Test]
        public void Draw_NoBanners_IsNull()
        {
            Assert.That(AdBoard.Draw(null, 0.3f), Is.Null);
            Assert.That(AdBoard.Draw(new Sprite[0], 0.3f), Is.Null);
        }
    }
}
