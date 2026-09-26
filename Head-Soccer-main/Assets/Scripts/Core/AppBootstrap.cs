using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Runs once before the first scene loads. Phones do not default to 60 fps, and a
    /// 30 fps kick feels late, so the frame rate is pinned here for every platform.
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

#if UNITY_ANDROID || UNITY_IOS
            // A match is short; never let the screen dim mid-rally.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif
        }
    }
}
