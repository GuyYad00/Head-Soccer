using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeadSoccer
{
    /// <summary>Menu.unity: pick a mode and a difficulty, then start a match.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string matchSceneName = "Match";
        [SerializeField] private TextMeshProUGUI difficultyLabel;
        [SerializeField] private TextMeshProUGUI audioLabel;

        private void Start()
        {
            RefreshLabels();
        }

        public void PlayVsCPU()
        {
            MatchSettings.Mode = GameMode.OnePlayerVsCPU;
            SceneManager.LoadScene(matchSceneName);
        }

        public void PlayTwoPlayers()
        {
            MatchSettings.Mode = GameMode.TwoPlayers;
            SceneManager.LoadScene(matchSceneName);
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
            RefreshLabels();
        }

        public void ToggleAudio()
        {
            AudioManager.Instance?.ToggleMute();
            RefreshLabels();
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
                audioLabel.text = muted ? "AUDIO: OFF" : "AUDIO: ON";
            }
        }
    }
}
