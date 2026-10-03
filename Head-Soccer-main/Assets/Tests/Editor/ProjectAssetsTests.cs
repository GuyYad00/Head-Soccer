using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HeadSoccer.Tests
{
    /// <summary>
    /// Guards on the shipped assets, not on code. These catch the mistakes that a
    /// compile cannot: a character dropped from the roster when the asset was
    /// re-saved, a tuning value that makes the match unplayable, a scene missing from
    /// the build list, or a commentator without his call. Each one is a bug we hit or
    /// nearly hit while building.
    /// </summary>
    public class ProjectAssetsTests
    {
        private const string RosterPath = "Assets/Data/CharacterRoster.asset";
        private const string ConfigPath = "Assets/Data/GameConfig.asset";

        private static readonly string[] ExpectedRoster = { "Yossi", "David", "Kim", "Mikel", "Noa", "Anna" };

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"{path} is missing or is not a {typeof(T).Name}");
            return asset;
        }

        // ------------------------------------------------------------ the roster

        [Test]
        public void Roster_HasAllSixCharacters_InMenuOrder()
        {
            CharacterRoster roster = Load<CharacterRoster>(RosterPath);
            string[] names = Enumerable.Range(0, roster.Count).Select(i => roster.Get(i).displayName).ToArray();
            Assert.That(names, Is.EqualTo(ExpectedRoster));
        }

        [Test]
        public void Roster_EveryCharacterHasAllThreeDrawings()
        {
            CharacterRoster roster = Load<CharacterRoster>(RosterPath);
            for (int i = 0; i < roster.Count; i++)
            {
                CharacterDefinition character = roster.Get(i);
                Assert.That(character.portrait, Is.Not.Null, $"{character.displayName} has no idle drawing");
                Assert.That(character.kickPose, Is.Not.Null, $"{character.displayName} has no kick drawing");
                Assert.That(character.celebration, Is.Not.Null, $"{character.displayName} has no celebration drawing");
                Assert.That(character.celebrationStyle, Is.Not.EqualTo(CelebrationStyle.None),
                    $"{character.displayName} has a celebration drawing but no motion for it");
            }
        }

        [Test]
        public void Roster_StatsStayInsideTheInspectorRange()
        {
            // The sliders allow 0.7 to 1.4; anything outside makes one character a cheat.
            CharacterRoster roster = Load<CharacterRoster>(RosterPath);
            for (int i = 0; i < roster.Count; i++)
            {
                CharacterDefinition character = roster.Get(i);
                Assert.That(character.speed, Is.InRange(0.7f, 1.4f), $"{character.displayName} speed");
                Assert.That(character.jump, Is.InRange(0.7f, 1.4f), $"{character.displayName} jump");
                Assert.That(character.power, Is.InRange(0.7f, 1.4f), $"{character.displayName} power");
            }
        }

        [Test]
        public void Roster_CharacterDrawingsAreImportedAsSprites()
        {
            CharacterRoster roster = Load<CharacterRoster>(RosterPath);
            for (int i = 0; i < roster.Count; i++)
            {
                string path = AssetDatabase.GetAssetPath(roster.Get(i).portrait);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, $"{path} has no texture importer");
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), $"{path} is not imported as a sprite");
            }
        }

        // ------------------------------------------------------------ the config

        [Test]
        public void Config_MatchCanActuallyEnd()
        {
            GameConfig config = Load<GameConfig>(ConfigPath);
            Assert.That(config.matchLength, Is.GreaterThan(0f), "a zero clock never ends");
            Assert.That(config.goalTarget, Is.GreaterThan(0), "a zero goal target ends the match at kickoff");
            Assert.That(config.kickoffCountFrom, Is.InRange(1, 5));
        }

        [Test]
        public void Config_KickAngleRangeIsOrderedAndAboveTheGrass()
        {
            GameConfig config = Load<GameConfig>(ConfigPath);
            Assert.That(config.kickMinAngle, Is.GreaterThanOrEqualTo(0f));
            Assert.That(config.kickMaxAngle, Is.LessThanOrEqualTo(90f));
            Assert.That(config.kickMinAngle, Is.LessThanOrEqualTo(config.kickMaxAngle));
        }

        [Test]
        public void Config_MovementAndKickNumbersArePositive()
        {
            GameConfig config = Load<GameConfig>(ConfigPath);
            Assert.That(config.moveSpeed, Is.GreaterThan(0f));
            Assert.That(config.jumpVelocity, Is.GreaterThan(0f));
            Assert.That(config.kickImpulse, Is.GreaterThan(0f));
            Assert.That(config.kickRange, Is.GreaterThan(0f));
            Assert.That(config.ballMaxSpeed, Is.GreaterThan(config.ballMinSpeed));
            Assert.That(config.superChargeTime, Is.GreaterThan(0f));
            Assert.That(config.kickMomentumBonus, Is.GreaterThanOrEqualTo(0f));
        }

        // ------------------------------------------------------------ scenes and build

        [Test]
        public void BuildSettings_ListMenuThenMatch_BothEnabled()
        {
            string[] enabled = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => Path.GetFileName(scene.path))
                .ToArray();
            Assert.That(enabled, Is.EqualTo(new[] { "Menu.unity", "Match.unity" }));
        }

        [Test]
        public void BuildSettings_EveryListedSceneExistsOnDisk()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                Assert.That(File.Exists(scene.path), Is.True, $"{scene.path} is in the build list but not in the project");
        }

        // ------------------------------------------------------------ the commentator

        [Test]
        public void Commentator_HasThreeCutOutDrawings()
        {
            for (int i = 1; i <= 3; i++)
            {
                string path = $"Assets/Art/Commentators/commentator_{i}.png";
                Sprite sprite = Load<Sprite>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer.alphaIsTransparency, Is.True, $"{path} should be a cut-out with transparency");
                Assert.That(sprite.rect.height, Is.GreaterThan(sprite.rect.width), $"{path} should be a portrait, taller than wide");
            }
        }

        [Test]
        public void AdBoard_HasTwoBannerStripsOfTheSameShape()
        {
            // AdBoard draws one per match with equal odds. Both must exist, and both
            // must be the same long strip, or one match gets a board of a different height.
            Sprite first = Load<Sprite>("Assets/Art/adboard_1.png");
            Sprite second = Load<Sprite>("Assets/Art/adboard_2.png");
            Assert.That(first.rect.width, Is.GreaterThan(first.rect.height * 8f), "a banner strip is much wider than tall");
            Assert.That(second.rect.size, Is.EqualTo(first.rect.size), "both strips share one size");
        }

        [Test]
        public void Commentator_GoalCallIsCutToSixSeconds()
        {
            // The call is trimmed to 0:00-0:06 on purpose: anything longer spills into play after kickoff.
            AudioClip call = Load<AudioClip>("Assets/Audio/commentator_goal.wav");
            Assert.That(call.length, Is.InRange(5.5f, 6.5f));
        }

        // ------------------------------------------------------------ the crowd

        [TestCase("Assets/Audio/crowd_sing_loop.ogg", 15f)]
        [TestCase("Assets/Audio/stadium_ambience_loop.ogg", 40f)]
        public void Crowd_LoopsShipAndAreLongEnoughNotToRepeatObviously(string path, float minSeconds)
        {
            // The menu sings, the match roars; both loop for as long as the scene is open,
            // so a short clip would give the seam away. The builder wires them by file name.
            AudioClip loop = Load<AudioClip>(path);
            Assert.That(loop.length, Is.GreaterThan(minSeconds), $"{path} is too short to loop unnoticed");
            Assert.That(loop.channels, Is.InRange(1, 2), $"{path} should be mono or stereo");
        }

        [Test]
        public void Victory_AnthemShipsAtAboutTenSeconds()
        {
            // Long enough to feel like a reward, short enough that the result screen is not stuck under it.
            AudioClip anthem = Load<AudioClip>("Assets/Audio/player_victory.ogg");
            Assert.That(anthem.length, Is.InRange(9f, 11f));
            Assert.That(anthem.channels, Is.EqualTo(2));
        }

        [Test]
        public void Defeat_ThemeShipsAtAboutTenSeconds()
        {
            AudioClip theme = Load<AudioClip>("Assets/Audio/player_defeat.ogg");
            Assert.That(theme.length, Is.InRange(9f, 11f));
            Assert.That(theme.channels, Is.EqualTo(2));
        }
    }
}
