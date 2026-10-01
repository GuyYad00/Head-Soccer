using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HeadSoccer
{
    /// <summary>
    /// Singleton. The single source of truth for match state, score and clock.
    /// Nothing else is allowed to change the score or the timer.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private GameConfig config;

        [Header("Scene references")]
        [SerializeField] private BallController ball;
        [SerializeField] private PlayerController leftPlayer;
        [SerializeField] private PlayerController rightPlayer;

        [Header("Scenes")]
        [SerializeField] private string menuSceneName = "Menu";

        public GameConfig Config => config;
        public BallController Ball => ball;
        public PlayerController LeftPlayer => leftPlayer;
        public PlayerController RightPlayer => rightPlayer;
        public MatchState State { get; private set; } = MatchState.Menu;

        /// <summary>False during kickoff, goal celebration, pause and match over.</summary>
        public bool ControlsEnabled { get; private set; }

        public int LeftScore { get; private set; }
        public int RightScore { get; private set; }
        public float TimeRemaining { get; private set; }

        /// <summary>-1 while undecided or on a draw, otherwise 0 for left and 1 for right.</summary>
        public int WinnerIndex { get; private set; } = -1;

        public event Action<MatchState> StateChanged;
        public event Action<int, int> ScoreChanged;
        /// <summary>Raised the instant a goal is awarded, before the celebration freeze.</summary>
        public event Action<Side> GoalScored;
        public event Action<string> CountdownChanged;
        public event Action<int> MatchEnded;

        private MatchState stateBeforePause;
        private bool acceptsMatchOverInput;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (config == null)
                Debug.LogError("GameManager has no GameConfig assigned.", this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            TimeRemaining = config != null ? config.matchLength : 90f;
            ControlsEnabled = false;
            ball?.Freeze();
            SetState(MatchState.Menu);
            ScoreChanged?.Invoke(LeftScore, RightScore);

            // Mode and character were chosen in Menu.unity, so the match starts straight
            // into the kickoff countdown.
            LaunchMatch();
        }

        /// <summary>Starts the kickoff countdown. Safe to call once per scene load.</summary>
        public void LaunchMatch()
        {
            if (State != MatchState.Menu) return;
            leftPlayer?.RebuildInput();
            rightPlayer?.RebuildInput();
            StartCoroutine(BeginMatch());
        }

        private IEnumerator BeginMatch()
        {
            // Wait one frame so every UI subscriber has finished its own Start before
            // the first score and countdown events are raised.
            yield return null;
            ScoreChanged?.Invoke(LeftScore, RightScore);
            yield return KickoffRoutine();
        }

        private void Update()
        {
            if (PausePressedThisFrame())
                TogglePause();

            if (State != MatchState.Playing) return;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                EndMatch();
            }
        }

        /// <summary>Esc on the keyboard or Start on any connected gamepad (GDD section 4).</summary>
        private static bool PausePressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
                return true;

            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
                if (pads[i].startButton.wasPressedThisFrame) return true;

            return false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // The GDD requires the match to pause when the window loses focus.
            if (!hasFocus && State != MatchState.Paused && State != MatchState.MatchOver && State != MatchState.Menu)
                TogglePause();
        }

        // ---------------------------------------------------------------- scoring

        /// <summary>Called by GoalTrigger. <paramref name="scorer"/> is who gets the point.</summary>
        public void ScoreGoal(Side scorer)
        {
            if (State != MatchState.Playing) return;

            if (scorer == Side.Left) LeftScore++;
            else RightScore++;

            // The deciding goal is the end, not the freeze that follows it. The song and
            // the menu loop start before anyone else can play over them.
            if (TargetReached())
                BeginResultAudio();

            ScoreChanged?.Invoke(LeftScore, RightScore);
            GoalScored?.Invoke(scorer);
            StartCoroutine(GoalRoutine(scorer));
        }

        private IEnumerator GoalRoutine(Side scorer)
        {
            SetState(MatchState.GoalScored);
            ControlsEnabled = false;
            ball.Freeze();

            AudioManager.Instance?.PlayGoal();
            EffectsPool.Instance?.SpawnGoalConfetti(ball.transform.position);
            CameraShake.Play(0.35f, 0.18f);
            CountdownChanged?.Invoke("GOAL!");

            yield return new WaitForSeconds(config.goalFreezeTime);

            // The GDD ends the match early when a player reaches the goal target.
            if (config.goalTarget > 0 && (LeftScore >= config.goalTarget || RightScore >= config.goalTarget))
            {
                EndMatch();
                yield break;
            }

            yield return KickoffRoutine();
        }

        // ---------------------------------------------------------------- flow

        private IEnumerator KickoffRoutine()
        {
            SetState(MatchState.Kickoff);
            ControlsEnabled = false;

            ball.ResetToCenter();
            leftPlayer.ResetToSpawn();
            rightPlayer.ResetToSpawn();

            for (int i = config.kickoffCountFrom; i > 0; i--)
            {
                CountdownChanged?.Invoke(i.ToString());
                AudioManager.Instance?.PlayCountdownBeep();
                yield return new WaitForSeconds(1f);
            }

            CountdownChanged?.Invoke("GO!");
            AudioManager.Instance?.PlayWhistle();

            ball.Unfreeze();
            ControlsEnabled = true;
            SetState(MatchState.Playing);

            yield return new WaitForSeconds(0.4f);
            CountdownChanged?.Invoke(string.Empty);
        }

        /// <summary>One-shot slow motion and shake after a connecting special.</summary>
        public void PlaySpecialWindow()
        {
            if (State == MatchState.Paused) return;
            StartCoroutine(SpecialWindow());
        }

        private IEnumerator SpecialWindow()
        {
            CameraShake.Play(0.28f, 0.14f);
            Time.timeScale = 0.28f;
            yield return new WaitForSecondsRealtime(config != null ? config.specialSlowMotionTime : 0.35f);
            if (State != MatchState.Paused)
                Time.timeScale = 1f;
        }

        /// <summary>A tiny freeze on a connecting kick, so the hit reads clearly.</summary>
        public void PlayKickHitStop()
        {
            if (State != MatchState.Playing || config == null || config.kickHitStop <= 0f) return;
            if (Time.timeScale < 0.99f) return;   // never stack on top of the Super slow motion
            StartCoroutine(HitStop(config.kickHitStop));
        }

        private IEnumerator HitStop(float seconds)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(seconds);
            if (State == MatchState.Playing)
                Time.timeScale = 1f;
        }

        private void EndMatch()
        {
            if (State == MatchState.MatchOver) return;

            SetState(MatchState.MatchOver);
            ControlsEnabled = false;
            Time.timeScale = 1f;
            ball.Freeze();
            CountdownChanged?.Invoke(string.Empty);

            BeginResultAudio();
            MatchRecords.RecordMatch(LeftScore, RightScore, WinnerIndex);
            MatchEnded?.Invoke(WinnerIndex);
            StartCoroutine(MatchOverInputLockout());
        }

        private IEnumerator MatchOverInputLockout()
        {
            acceptsMatchOverInput = false;
            yield return new WaitForSecondsRealtime(config.matchOverInputLockout);
            acceptsMatchOverInput = true;
        }

        /// <summary>The REMATCH and MENU buttons check this so the last kick cannot skip the result.</summary>
        public bool CanUseMatchOverButtons => State == MatchState.MatchOver && acceptsMatchOverInput;

        /// <summary>True once the deciding moment has started the result audio. The final goal must not talk over it.</summary>
        public bool ResultAudioStarted { get; private set; }

        private bool TargetReached() =>
            config != null && config.goalTarget > 0 && (LeftScore >= config.goalTarget || RightScore >= config.goalTarget);

        /// <summary>
        /// Once, on the frame the match is decided. A later call from the freeze is a no-op,
        /// so the song is not delayed by the goal celebration.
        /// </summary>
        private void BeginResultAudio()
        {
            if (ResultAudioStarted) return;
            ResultAudioStarted = true;

            if (LeftScore > RightScore) WinnerIndex = 0;
            else if (RightScore > LeftScore) WinnerIndex = 1;
            else WinnerIndex = -1;

            AudioManager.Instance?.BeginResult(WinnerIndex);
        }

        // ---------------------------------------------------------------- pause

        public void TogglePause()
        {
            if (State == MatchState.MatchOver || State == MatchState.Menu) return;

            if (State == MatchState.Paused)
            {
                Time.timeScale = 1f;
                SetState(stateBeforePause);
                ControlsEnabled = stateBeforePause == MatchState.Playing;
            }
            else
            {
                stateBeforePause = State;
                Time.timeScale = 0f;
                ControlsEnabled = false;
                SetState(MatchState.Paused);
            }
        }

        public void Resume()
        {
            if (State == MatchState.Paused) TogglePause();
        }

        public void RestartMatch()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuSceneName);
        }

        private void SetState(MatchState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
