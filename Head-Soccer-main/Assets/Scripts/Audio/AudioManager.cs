using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// One-shot SFX and looping music. Every clip field is optional, so the game runs
    /// silently and without errors until the audio assets are dropped in.
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

        [Header("Music")]
        [SerializeField] private AudioClip musicLoop;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.4f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

        private AudioSource sfxSource;
        private AudioSource voiceSource;
        private AudioSource musicSource;

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

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;

            IsMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            ApplyMute();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (musicLoop == null) return;
            musicSource.clip = musicLoop;
            musicSource.Play();
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

        /// <summary>The commentator's call. Restarts if he is still shouting the last goal.</summary>
        public void PlayCommentator()
        {
            if (commentatorGoal == null || voiceSource == null) return;
            voiceSource.Stop();
            voiceSource.clip = commentatorGoal;
            voiceSource.volume = sfxVolume;
            voiceSource.Play();
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
