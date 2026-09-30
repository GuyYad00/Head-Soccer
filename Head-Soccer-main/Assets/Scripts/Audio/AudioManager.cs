using System.Collections;
using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// One-shot SFX, the commentator's voice channel, the victory anthem and the looping
    /// crowd. Each scene carries its own AudioManager with its own crowd loop: the menu
    /// has the stands singing, the match has the stadium roar. Every clip field is
    /// optional, so the game runs silently and without errors until the audio assets are dropped in.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string MutedKey = "hs_audio_muted";

        [Header("SFX")]
        [SerializeField] private AudioClip kick;
        [SerializeField] private AudioClip bounce;
        [SerializeField] private AudioClip jump;
        [SerializeField] private AudioClip whistle;
        [SerializeField] private AudioClip goal;
        [SerializeField] private AudioClip crowdCheer;
        [SerializeField] private AudioClip countdownBeep;
        [SerializeField] private AudioClip special;
        [SerializeField] private AudioClip uiClick;

        [Header("Voice")]
        [Tooltip("The commentator's goal call. Played on its own channel so a second goal cuts the first call instead of stacking on it.")]
        [SerializeField] private AudioClip commentatorGoal;
        [Tooltip("The call plays alone over a muted game mix, so it takes the whole headroom.")]
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;

        [Header("Victory")]
        [Tooltip("Plays only when the human beats the CPU, on the voice channel, with the crowd and the effects silenced under it.")]
        [SerializeField] private AudioClip playerVictory;
        [SerializeField, Range(0f, 1f)] private float victoryVolume = 1f;

        [Header("Crowd")]
        [Tooltip("This scene's crowd, looped without a break: the stands singing in the menu, the stadium roar in the match. Both are seamless loops, so the seam is never heard.")]
        [SerializeField] private AudioClip crowdLoop;
        [SerializeField, Range(0f, 1f)] private float crowdVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

        // Seconds to cut the call at kickoff and to bring the game mix back afterwards.
        private const float VoiceCutSeconds = 0.12f;
        private const float MixRestoreSeconds = 0.3f;

        private AudioSource sfxSource;
        private AudioSource voiceSource;
        private AudioSource crowdSource;
        private Coroutine mixRoutine;

        public bool IsMuted { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;

            crowdSource = gameObject.AddComponent<AudioSource>();
            crowdSource.playOnAwake = false;
            crowdSource.loop = true;
            crowdSource.volume = crowdVolume;

            IsMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            ApplyMute();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (crowdLoop == null) return;
            crowdSource.clip = crowdLoop;
            crowdSource.Play();
        }

        public void PlayKick() => PlayOneShot(kick);
        public void PlayBounce() => PlayOneShot(bounce);
        public void PlayJump() => PlayOneShot(jump, 0.5f);
        public void PlayWhistle() => PlayOneShot(whistle);
        public void PlayCountdownBeep() => PlayOneShot(countdownBeep, 0.6f);
        public void PlaySpecial() => PlayOneShot(special);
        public void PlayUiClick() => PlayOneShot(uiClick, 0.6f);

        public void PlayGoal()
        {
            PlayOneShot(goal);
            PlayOneShot(crowdCheer, 0.7f);
        }

        /// <summary>
        /// The commentator's call, alone in the mix: the crowd and the SFX are ducked to
        /// silence while he shouts, the way a broadcast drops the stadium feed under the booth.
        /// Restarts if he is still shouting the last goal. The mix comes back when the
        /// call ends or when <see cref="StopCommentator"/> cuts it at kickoff.
        /// </summary>
        public void PlayCommentator()
        {
            if (commentatorGoal == null || voiceSource == null) return;

            if (mixRoutine != null) StopCoroutine(mixRoutine);
            voiceSource.Stop();
            voiceSource.clip = commentatorGoal;
            voiceSource.volume = voiceVolume;
            voiceSource.Play();

            crowdSource.volume = 0f;
            sfxSource.volume = 0f;
            mixRoutine = StartCoroutine(RestoreMixWhenCallEnds());
        }

        /// <summary>
        /// The anthem for beating the CPU, alone in the mix: the commentator is cut,
        /// the crowd and the effects go silent, and only the song is heard until it ends.
        /// Returns false when this result does not earn it, so the caller plays the whistle.
        /// </summary>
        public bool PlayHumanVictoryOverCpu(int winnerIndex)
        {
            if (playerVictory == null || voiceSource == null) return false;
            if (!MatchSettings.HumanBeatTheCpu(MatchSettings.Mode, winnerIndex)) return false;

            if (mixRoutine != null) StopCoroutine(mixRoutine);
            voiceSource.Stop();
            sfxSource.Stop();

            voiceSource.clip = playerVictory;
            voiceSource.volume = victoryVolume;
            voiceSource.Play();

            crowdSource.volume = 0f;
            sfxSource.volume = 0f;
            mixRoutine = StartCoroutine(RestoreMixWhenCallEnds());
            return true;
        }

        /// <summary>Cuts the call with a short fade and brings the game mix back.</summary>
        public void StopCommentator()
        {
            if (voiceSource == null || !voiceSource.isPlaying) return;
            if (mixRoutine != null) StopCoroutine(mixRoutine);
            mixRoutine = StartCoroutine(FadeOutCallAndRestoreMix());
        }

        private IEnumerator RestoreMixWhenCallEnds()
        {
            while (voiceSource.isPlaying) yield return null;
            yield return RestoreMix();
        }

        private IEnumerator FadeOutCallAndRestoreMix()
        {
            float startVolume = voiceSource.volume;
            for (float t = 0f; t < VoiceCutSeconds; t += Time.unscaledDeltaTime)
            {
                voiceSource.volume = Mathf.Lerp(startVolume, 0f, t / VoiceCutSeconds);
                yield return null;
            }
            voiceSource.Stop();
            yield return RestoreMix();
        }

        /// <summary>Ramps the crowd and SFX back up. Unscaled time, since the Super slow motion plays with Time.timeScale.</summary>
        private IEnumerator RestoreMix()
        {
            for (float t = 0f; t < MixRestoreSeconds; t += Time.unscaledDeltaTime)
            {
                float u = t / MixRestoreSeconds;
                crowdSource.volume = crowdVolume * u;
                sfxSource.volume = u;
                yield return null;
            }
            crowdSource.volume = crowdVolume;
            sfxSource.volume = 1f;
            mixRoutine = null;
        }

        private void PlayOneShot(AudioClip clip, float scale = 1f)
        {
            if (clip == null || sfxSource == null) return;
            // Unscaled by design: SFX still fire during the goal freeze.
            sfxSource.PlayOneShot(clip, sfxVolume * scale);
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            PlayerPrefs.SetInt(MutedKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMute();
        }

        private void ApplyMute()
        {
            AudioListener.volume = IsMuted ? 0f : 1f;
        }
    }
}
