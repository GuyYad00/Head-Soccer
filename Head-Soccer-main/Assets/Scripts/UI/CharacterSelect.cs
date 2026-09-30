using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeadSoccer
{
    /// <summary>
    /// The Character Select panel in Menu.unity: a portrait in the middle, arrows to
    /// change character, the name and a stat hint, and a confirm button.
    /// It runs twice before a match. First the left side is chosen (player one), then
    /// the right side: player two picks for himself in a 2 PLAYERS match, and against
    /// the CPU player one also decides who the computer plays as. Both choices are
    /// saved through CharacterRoster so Match.unity can read them.
    /// </summary>
    public class CharacterSelect : MonoBehaviour
    {
        private enum Step { LeftPlayer, RightPlayer }

        [SerializeField] private CharacterRoster roster;
        [SerializeField] private MainMenuController menu;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image portrait;
        [Tooltip("Optional. Lives on the portrait and loops the character's celebration.")]
        [SerializeField] private CelebrationLoop celebration;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statText;
        [SerializeField] private Image speedBar;
        [SerializeField] private Image jumpBar;
        [SerializeField] private Image powerBar;
        [SerializeField] private TextMeshProUGUI confirmLabel;

        private Step step;
        private int index;

        private void OnEnable()
        {
            step = Step.LeftPlayer;
            LoadIndexForStep();
            Refresh();
        }

        public void Next() => Move(1);

        public void Previous() => Move(-1);

        /// <summary>NEXT after the left player, KICK OFF! after the right one.</summary>
        public void Confirm()
        {
            if (step == Step.LeftPlayer)
            {
                AudioManager.Instance?.PlayUiClick();
                step = Step.RightPlayer;
                LoadIndexForStep();
                Refresh();
                return;
            }

            menu?.StartMatch();
        }

        /// <summary>Back from the right player returns to the left one, not to the menu.</summary>
        public void Back()
        {
            if (step == Step.RightPlayer)
            {
                AudioManager.Instance?.PlayUiClick();
                step = Step.LeftPlayer;
                LoadIndexForStep();
                Refresh();
                return;
            }

            menu?.BackToMain();
        }

        private void Move(int direction)
        {
            if (roster == null || roster.Count == 0) return;
            index = CharacterRoster.Wrap(index, direction, roster.Count);
            SaveIndexForStep();
            AudioManager.Instance?.PlayCountdownBeep();
            Refresh();
        }

        private void LoadIndexForStep()
        {
            int saved = step == Step.LeftPlayer ? CharacterRoster.SelectedIndex : CharacterRoster.OpponentIndex;
            index = roster != null ? Mathf.Clamp(saved, 0, Mathf.Max(0, roster.Count - 1)) : 0;
        }

        private void SaveIndexForStep()
        {
            if (step == Step.LeftPlayer) CharacterRoster.SelectedIndex = index;
            else CharacterRoster.OpponentIndex = index;
        }

        private void Refresh()
        {
            if (roster == null || roster.Count == 0) return;
            CharacterDefinition character = roster.Get(index);

            if (titleText != null) titleText.text = TitleForStep();
            if (confirmLabel != null) confirmLabel.text = step == Step.LeftPlayer ? "NEXT" : "KICK OFF!";

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

        private string TitleForStep()
        {
            if (step == Step.LeftPlayer) return "PLAYER 1: PICK YOUR PLAYER";
            return MatchSettings.Mode == GameMode.OnePlayerVsCPU
                ? "PICK THE CPU'S PLAYER"
                : "PLAYER 2: PICK YOUR PLAYER";
        }

        private static void SetBar(Image bar, float multiplier)
        {
            if (bar == null) return;
            bar.fillAmount = Mathf.InverseLerp(0.7f, 1.4f, multiplier);
        }
    }
}
