using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// How a character celebrates on the Character Select screen. The sprite is one
    /// frame; the style tells CelebrationLoop which motion to add on top of it.
    /// </summary>
    public enum CelebrationStyle
    {
        None,
        /// <summary>Kneels and kisses the badge on the shirt.</summary>
        KissBadge,
        /// <summary>Hands form a heart that beats.</summary>
        Heart,
        /// <summary>A deep, respectful bow.</summary>
        Bow,
        /// <summary>A full backflip in the air.</summary>
        Flip,
        /// <summary>Both arms shoot skyward and the whole body bounces with joy.</summary>
        Cheer,
        /// <summary>Drops down and glides across the grass on the knees.</summary>
        KneeSlide
    }

    /// <summary>One selectable character: a name, a look and three small stat multipliers.</summary>
    [System.Serializable]
    public struct CharacterDefinition
    {
        public string displayName;
        [TextArea] public string statHint;
        public Sprite portrait;
        [Tooltip("Optional. Shown for a moment on every kick. Leave empty to reuse the portrait.")]
        public Sprite kickPose;
        [Tooltip("Optional. The celebration frame looped on the Character Select screen only.")]
        public Sprite celebration;
        public CelebrationStyle celebrationStyle;
        [Tooltip("Which way the drawing looks, so PlayerVisual can flip it correctly.")]
        public bool facesRight;
        [Tooltip("Tints the portrait so two characters can share one drawing.")]
        public Color tint;
        [Range(0.7f, 1.4f)] public float speed;
        [Range(0.7f, 1.4f)] public float jump;
        [Range(0.7f, 1.4f)] public float power;
    }

    /// <summary>
    /// The roster shown on the Character Select screen, and both saved choices: the
    /// character on the left (player one) and the one on the right (player two or the
    /// CPU). Kept small on purpose: the GDD rules out a roster of eight or more.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterRoster", menuName = "Head Soccer/Character Roster")]
    public class CharacterRoster : ScriptableObject
    {
        private const string LeftChoiceKey = "hs_character_p1";
        private const string RightChoiceKey = "hs_character_p2";

        [SerializeField] private CharacterDefinition[] characters = System.Array.Empty<CharacterDefinition>();

        public int Count => characters.Length;

        public CharacterDefinition Get(int index)
        {
            if (characters.Length == 0) return default;
            index = Mathf.Clamp(index, 0, characters.Length - 1);
            return characters[index];
        }

        /// <summary>Player one's choice, saved in PlayerPrefs, the only save system the GDD allows.</summary>
        public static int SelectedIndex
        {
            get => PlayerPrefs.GetInt(LeftChoiceKey, 0);
            set { PlayerPrefs.SetInt(LeftChoiceKey, value); PlayerPrefs.Save(); }
        }

        /// <summary>
        /// The right-hand character. In a 2 PLAYERS match player two picks it; against
        /// the CPU player one picks who the computer plays as. Defaults to the second
        /// character so a first match is never a mirror match.
        /// </summary>
        public static int OpponentIndex
        {
            get => PlayerPrefs.GetInt(RightChoiceKey, 1);
            set { PlayerPrefs.SetInt(RightChoiceKey, value); PlayerPrefs.Save(); }
        }

        public CharacterDefinition Selected => Get(SelectedIndex);

        public CharacterDefinition Opponent => Get(OpponentIndex);
    }
}
