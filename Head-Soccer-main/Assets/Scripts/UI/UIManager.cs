using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeadSoccer
{
    /// <summary>
    /// The whole in-match UI: scoreboard, clock, Super meters, kickoff countdown,
    /// pause overlay and the match over screen. It only reads state from GameManager
    /// and never changes it.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI leftScoreText;
        [SerializeField] private TextMeshProUGUI rightScoreText;
        [SerializeField] private TextMeshProUGUI leftNameText;
        [SerializeField] private TextMeshProUGUI rightNameText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private Image leftSuperFill;
        [SerializeField] private Image rightSuperFill;
        [SerializeField] private TextMeshProUGUI leftSuperLabel;
        [SerializeField] private TextMeshProUGUI rightSuperLabel;

        [Header("Juice")]
        [SerializeField] private Image goalFlash;
        [SerializeField] private Color leftGoalColor = new Color(1f, 0.35f, 0.3f, 0.6f);
        [SerializeField] private Color rightGoalColor = new Color(0.35f, 0.6f, 1f, 0.6f);
        [SerializeField] private Color superReadyColor = new Color(1f, 0.82f, 0.2f, 1f);
        [SerializeField] private Color superChargingColor = new Color(0.45f, 0.85f, 1f, 1f);

        [Header("Overlays")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject matchOverPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI recordText;

        [Header("Touch")]
        [SerializeField] private GameObject touchControlsPanel;
        [SerializeField] private bool forceTouchControlsInEditor;

        private GameManager game;
        private int lastShownSeconds = -1;

        private void Awake()
        {
            Instance = this;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (matchOverPanel != null) matchOverPanel.SetActive(false);
            if (goalFlash != null) goalFlash.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (game == null) return;

            game.ScoreChanged -= OnScoreChanged;
            game.GoalScored -= OnGoalScored;
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
            game.GoalScored += OnGoalScored;
            game.StateChanged += OnStateChanged;
            game.CountdownChanged += OnCountdownChanged;
            game.MatchEnded += OnMatchEnded;

            if (pausePanel != null) pausePanel.SetActive(false);
            if (matchOverPanel != null) matchOverPanel.SetActive(false);
            if (countdownText != null) countdownText.text = string.Empty;

            if (touchControlsPanel != null)
                touchControlsPanel.SetActive(Application.isMobilePlatform || forceTouchControlsInEditor);

            SetName(leftNameText, game.LeftPlayer, "P1");
            SetName(rightNameText, game.RightPlayer,
                MatchSettings.Mode == GameMode.OnePlayerVsCPU ? "CPU" : "P2");

            OnScoreChanged(game.LeftScore, game.RightScore);
        }

        private static void SetName(TextMeshProUGUI label, PlayerController player, string fallback)
        {
            if (label == null) return;
            string name = player != null ? player.CharacterName : string.Empty;
            label.text = string.IsNullOrEmpty(name) ? fallback : name.ToUpperInvariant();
        }

        private void Update()
        {
            if (game == null) return;

            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(game.TimeRemaining);
                if (seconds != lastShownSeconds)
                {
                    lastShownSeconds = seconds;
                    timerText.text = $"{seconds / 60:0}:{seconds % 60:00}";
                    // The last ten seconds turn red and tick, so the ending is never a surprise.
                    bool closing = seconds <= 10 && game.State == MatchState.Playing;
                    timerText.color = closing ? new Color(1f, 0.4f, 0.35f) : Color.white;
                    if (closing) StartCoroutine(Bump(timerText.rectTransform, 1.18f, 0.18f));
                }
            }

            RefreshSuper(game.LeftPlayer, leftSuperFill, leftSuperLabel);
            RefreshSuper(game.RightPlayer, rightSuperFill, rightSuperLabel);
        }

        /// <summary>
        /// Text is only assigned when the state changes; setting TMP text every frame
        /// rebuilds the mesh every frame for nothing.
        /// </summary>
        private void RefreshSuper(PlayerController player, Image fill, TextMeshProUGUI label)
        {
            if (player == null) return;

            if (player.SpecialUsed)
            {
                if (fill != null) fill.fillAmount = 0f;
                if (label != null)
                {
                    SetTextIfChanged(label, "USED");
                    label.color = new Color(1f, 1f, 1f, 0.35f);
                }
                return;
            }

            if (fill != null) fill.fillAmount = player.SpecialCharge;
            if (label == null) return;

            if (player.SpecialReady)
            {
                // Pulses so the eye is drawn to it the moment it becomes available.
                float pulse = 0.75f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.25f;
                SetTextIfChanged(label, "SUPER READY");
                label.color = superReadyColor * new Color(1f, 1f, 1f, pulse);
                if (fill != null) fill.color = superReadyColor;
            }
            else
            {
                SetTextIfChanged(label, "SUPER");
                label.color = new Color(1f, 1f, 1f, 0.55f);
                if (fill != null) fill.color = superChargingColor;
            }
        }

        private static void SetTextIfChanged(TMP_Text label, string value)
        {
            if (!ReferenceEquals(label.text, value) && label.text != value)
                label.text = value;
        }

        // ---------------------------------------------------------------- events

        private void OnScoreChanged(int left, int right)
        {
            if (leftScoreText != null) leftScoreText.text = left.ToString();
            if (rightScoreText != null) rightScoreText.text = right.ToString();
        }

        private void OnGoalScored(Side scorer)
        {
            TextMeshProUGUI scoreText = scorer == Side.Left ? leftScoreText : rightScoreText;
            if (scoreText != null) StartCoroutine(Bump(scoreText.rectTransform, 1.6f, 0.45f));

            if (goalFlash != null)
                StartCoroutine(Flash(scorer == Side.Left ? leftGoalColor : rightGoalColor, 0.5f));
        }

        private void OnCountdownChanged(string text)
        {
            if (countdownText == null) return;
            countdownText.text = text;
            if (!string.IsNullOrEmpty(text))
                StartCoroutine(Bump(countdownText.rectTransform, 1.35f, 0.3f));
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
                string left = leftNameText != null ? leftNameText.text : "PLAYER 1";
                string right = rightNameText != null ? rightNameText.text : "PLAYER 2";
                winnerText.text = winnerIndex switch
                {
                    0 => $"{left} WINS!",
                    1 => $"{right} WINS!",
                    _ => "DRAW"
                };
            }

            if (finalScoreText != null)
                finalScoreText.text = $"{game.LeftScore}  -  {game.RightScore}";

            if (recordText != null)
                recordText.text = MatchRecords.Summary().Replace("\n", "     ");
        }

        // ---------------------------------------------------------------- juice

        /// <summary>Scales a rect up and eases it back. Unscaled so it works during the goal freeze.</summary>
        private static IEnumerator Bump(RectTransform target, float peakScale, float duration)
        {
            if (target == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Fast out, slow back: a quick pop that settles.
                float ease = 1f - (1f - t) * (1f - t);
                float scale = Mathf.Lerp(peakScale, 1f, ease);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            if (target != null) target.localScale = Vector3.one;
        }

        private IEnumerator Flash(Color color, float duration)
        {
            goalFlash.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(color.a, 0f, elapsed / duration);
                goalFlash.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
            goalFlash.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- buttons

        public void OnPauseButton()
        {
            AudioManager.Instance?.PlayUiClick();
            game?.TogglePause();
        }

        public void OnResumeButton()
        {
            AudioManager.Instance?.PlayUiClick();
            game?.Resume();
        }

        public void OnRestartButton()
        {
            if (game == null) return;
            // The lockout only applies to the match over screen, not to the pause menu.
            if (game.State == MatchState.MatchOver && !game.CanUseMatchOverButtons) return;
            AudioManager.Instance?.PlayUiClick();
            game.RestartMatch();
        }

        public void OnQuitToMenuButton()
        {
            if (game == null) return;
            if (game.State == MatchState.MatchOver && !game.CanUseMatchOverButtons) return;
            AudioManager.Instance?.PlayUiClick();
            game.QuitToMenu();
        }
    }
}
