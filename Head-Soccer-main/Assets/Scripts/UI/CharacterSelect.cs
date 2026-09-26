using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeadSoccer
{
    /// <summary>
    /// The Character Select panel in Menu.unity: a portrait in the middle, arrows to
    /// change character, the name and a stat hint, and CONFIRM which starts the match.
    /// The choice is saved through CharacterRoster so Match.unity can read it.
    /// </summary>
    public class CharacterSelect : MonoBehaviour
    {
        [SerializeField] private CharacterRoster roster;
        [SerializeField] private Image portrait;
        [Tooltip("Optional. Lives on the portrait and loops the character's celebration.")]
        [SerializeField] private CelebrationLoop celebration;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statText;
        [SerializeField] private Image speedBar;
        [SerializeField] private Image jumpBar;
        [SerializeField] private Image powerBar;

        private int index;

        private void OnEnable()
        {
            index = roster != null ? Mathf.Clamp(CharacterRoster.SelectedIndex, 0, Mathf.Max(0, roster.Count - 1)) : 0;
            Refresh();
        }

        public void Next() => Step(1);

        public void Previous() => Step(-1);

        private void Step(int direction)
        {
            if (roster == null || roster.Count == 0) return;
            index = (index + direction + roster.Count) % roster.Count;
            CharacterRoster.SelectedIndex = index;
            AudioManager.Instance?.PlayCountdownBeep();
            Refresh();
        }

        private void Refresh()
        {
            if (roster == null || roster.Count == 0) return;
            CharacterDefinition character = roster.Get(index);

            if (portrait != null)
            {
                portrait.sprite = character.portrait;
                portrait.color = character.tint;
                portrait.preserveAspect = true;
            }

            // The celebration restarts from idle on every change so the swap never lands mid-flip.
            if (celebration != null) celebration.Play(character);

            if (nameText != null) nameText.text = character.displayName;
            if (statText != null) statText.text = character.statHint;

            // Bars read 0.7..1.4 as empty..full so small differences are still visible.
            SetBar(speedBar, character.speed);
            SetBar(jumpBar, character.jump);
            SetBar(powerBar, character.power);
        }

        private static void SetBar(Image bar, float multiplier)
        {
            if (bar == null) return;
            bar.fillAmount = Mathf.InverseLerp(0.7f, 1.4f, multiplier);
        }
    }
}
