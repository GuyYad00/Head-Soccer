using TMPro;
using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// The whole in-match UI: scoreboard, clock, kickoff countdown, pause overlay and
    /// the match over screen. It only reads state from GameManager and never changes it.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI leftScoreText;
        [SerializeField] private TextMeshProUGUI rightScoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Overlays")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject matchOverPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI finalScoreText;

        [Header("Touch")]
        [SerializeField] private GameObject touchControlsPanel;
        [SerializeField] private bool forceTouchControlsInEditor;

        private GameManager game;

        private void Awake()
        {
            Instance = this;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (matchOverPanel != null) matchOverPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (game == null) return;

            game.ScoreChanged -= OnScoreChanged;
            game.StateChanged -= OnStateChanged;
            game.CountdownChanged -= OnCountdownChanged;
            game.MatchEnded -= OnMatchEnded;
        }

        private void Start()
        {
            game = GameManager.Instance;
            if (game == null)
            {
                Debug.LogError("UIManager found no GameManager in the scene.", this);
                return;
            }

            game.ScoreChanged += OnScoreChanged;
            game.StateChanged += OnStateChanged;
            game.CountdownChanged += OnCountdownChanged;
            game.MatchEnded += OnMatchEnded;

            if (pausePanel != null) pausePanel.SetActive(false);
            if (matchOverPanel != null) matchOverPanel.SetActive(false);
            if (countdownText != null) countdownText.text = string.Empty;

            if (touchControlsPanel != null)
                touchControlsPanel.SetActive(Application.isMobilePlatform || forceTouchControlsInEditor);

            OnScoreChanged(game.LeftScore, game.RightScore);
        }

        private void Update()
        {
            if (game == null || timerText == null) return;

            int seconds = Mathf.CeilToInt(game.TimeRemaining);
            timerText.text = $"{seconds / 60:0}:{seconds % 60:00}";
        }

        // ---------------------------------------------------------------- events

        private void OnScoreChanged(int left, int right)
        {
            if (leftScoreText != null) leftScoreText.text = left.ToString();
            if (rightScoreText != null) rightScoreText.text = right.ToString();
        }

        private void OnCountdownChanged(string text)
        {
            if (countdownText != null) countdownText.text = text;
        }

        private void OnStateChanged(MatchState state)
        {
            if (pausePanel != null)
                pausePanel.SetActive(state == MatchState.Paused);
        }

        private void OnMatchEnded(int winnerIndex)
        {
            if (matchOverPanel != null) matchOverPanel.SetActive(true);

            if (winnerText != null)
            {
                winnerText.text = winnerIndex switch
                {
                    0 => "PLAYER 1 WINS",
                    1 => "PLAYER 2 WINS",
                    _ => "DRAW"
                };
            }

            if (finalScoreText != null)
                finalScoreText.text = $"{game.LeftScore} - {game.RightScore}";
        }

        // ---------------------------------------------------------------- buttons

        public void OnPauseButton() => game?.TogglePause();

        public void OnResumeButton() => game?.Resume();

        public void OnRestartButton()
        {
            if (game == null) return;
            // The lockout only applies to the match over screen, not to the pause menu.
            if (game.State == MatchState.MatchOver && !game.CanUseMatchOverButtons) return;
            game.RestartMatch();
        }

        public void OnQuitToMenuButton()
        {
            if (game == null) return;
            if (game.State == MatchState.MatchOver && !game.CanUseMatchOverButtons) return;
            game.QuitToMenu();
        }
    }
}
