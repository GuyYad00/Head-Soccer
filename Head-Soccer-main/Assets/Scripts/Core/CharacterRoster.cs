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
        Flip
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
    /// The roster shown on the Character Select screen, and the player's saved choice.
    /// Kept small on purpose: the GDD limits the game to two or three characters.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterRoster", menuName = "Head Soccer/Character Roster")]
    public class CharacterRoster : ScriptableObject
    {
        private const string ChoiceKey = "hs_character_p1";

        [SerializeField] private CharacterDefinition[] characters = System.Array.Empty<CharacterDefinition>();

        public int Count => characters.Length;

        public CharacterDefinition Get(int index)
        {
            if (characters.Length == 0) return default;
            index = Mathf.Clamp(index, 0, characters.Length - 1);
            return characters[index];
        }

        /// <summary>Index saved in PlayerPrefs, the only save system the GDD allows.</summary>
        public static int SelectedIndex
        {
            get => PlayerPrefs.GetInt(ChoiceKey, 0);
            set { PlayerPrefs.SetInt(ChoiceKey, value); PlayerPrefs.Save(); }
        }

        public CharacterDefinition Selected => Get(SelectedIndex);

        /// <summary>The CPU / second player takes the other character so the two never look alike.</summary>
        public CharacterDefinition Opponent => Count > 1 ? Get((SelectedIndex + 1) % Count) : Selected;
    }
}
