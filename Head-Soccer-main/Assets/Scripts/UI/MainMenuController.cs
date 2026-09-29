using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeadSoccer
{
    /// <summary>
    /// Menu.unity. The main panel picks a mode, the character panel picks a look and
    /// confirms into the match. Difficulty and audio are remembered in MatchSettings
    /// and PlayerPrefs.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string matchSceneName = "Match";
        [SerializeField] private TextMeshProUGUI difficultyLabel;
        [SerializeField] private TextMeshProUGUI audioLabel;
        [SerializeField] private TextMeshProUGUI controlsLabel;
        [Tooltip("The one-line key reminder at the bottom of the menu; follows the chosen layout.")]
        [SerializeField] private TextMeshProUGUI controlsHint;

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject characterPanel;

        private void Start()
        {
            ShowMain();
            RefreshLabels();
        }

        // --- main panel -------------------------------------------------------

        public void PlayVsCPU()
        {
            MatchSettings.Mode = GameMode.OnePlayerVsCPU;
            OpenCharacterSelect();
        }

        public void PlayTwoPlayers()
        {
            MatchSettings.Mode = GameMode.TwoPlayers;
            OpenCharacterSelect();
        }

        public void CycleDifficulty()
        {
            AIDifficulty next = MatchSettings.Difficulty switch
            {
                AIDifficulty.Easy => AIDifficulty.Medium,
                AIDifficulty.Medium => AIDifficulty.Hard,
                _ => AIDifficulty.Easy
            };
            MatchSettings.Difficulty = next;
            AudioManager.Instance?.PlayUiClick();
            RefreshLabels();
        }

        public void ToggleAudio()
        {
            AudioManager.Instance?.ToggleMute();
            AudioManager.Instance?.PlayUiClick();
            RefreshLabels();
        }

        /// <summary>
        /// Swaps the keyboard layouts between the two players. Some people grew up on
        /// WASD, some on the arrows, so the choice is theirs, the way FIFA offers
        /// Classic and Alternate.
        /// </summary>
        public void ToggleControls()
        {
            MatchSettings.PlayerOneLayout = MatchSettings.PlayerOneLayout == KeyboardLayout.Wasd
                ? KeyboardLayout.Arrows
                : KeyboardLayout.Wasd;
            AudioManager.Instance?.PlayUiClick();
            RefreshLabels();
        }

        // --- character panel --------------------------------------------------

        public void OpenCharacterSelect()
        {
            AudioManager.Instance?.PlayUiClick();
            if (characterPanel == null)
            {
                StartMatch();
                return;
            }
            if (mainPanel != null) mainPanel.SetActive(false);
            characterPanel.SetActive(true);
        }

        public void ShowMain()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (characterPanel != null) characterPanel.SetActive(false);
        }

        public void BackToMain()
        {
            AudioManager.Instance?.PlayUiClick();
            ShowMain();
        }

        public void StartMatch()
        {
            AudioManager.Instance?.PlayWhistle();
            SceneManager.LoadScene(matchSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void RefreshLabels()
        {
            if (difficultyLabel != null)
                difficultyLabel.text = $"CPU: {MatchSettings.Difficulty.ToString().ToUpper()}";

            if (audioLabel != null)
            {
                bool muted = AudioManager.Instance != null && AudioManager.Instance.IsMuted;
                audioLabel.text = muted ? "SOUND: OFF" : "SOUND: ON";
            }

            KeyboardLayout one = MatchSettings.PlayerOneLayout;
            KeyboardLayout two = MatchSettings.PlayerTwoLayout;

            if (controlsLabel != null)
                controlsLabel.text = $"P1 KEYS: {one.ToString().ToUpper()}";

            if (controlsHint != null)
            {
                controlsHint.text =
                    $"P1   {KeyboardInputSource.Describe(one)}          P2   {KeyboardInputSource.Describe(two)}";
            }
        }
    }
}
