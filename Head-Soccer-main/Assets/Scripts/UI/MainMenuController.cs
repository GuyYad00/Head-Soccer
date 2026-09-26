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
        }
    }
}
