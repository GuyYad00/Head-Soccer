using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HeadSoccer.EditorTools
{
    /// <summary>
    /// One-click builds from the "Head Soccer" menu. Output lands in Builds/, which
    /// is ignored by git. Android needs the Android Build Support module installed.
    /// </summary>
    public static class HeadSoccerBuildPipeline
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Menu.unity",
            "Assets/Scenes/Match.unity"
        };

        [MenuItem("Head Soccer/Build Android APK", priority = 40)]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            Build(BuildTarget.Android, "Builds/Android/HeadSoccer.apk");
        }

        [MenuItem("Head Soccer/Build Windows (x64)", priority = 41)]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/HeadSoccer.exe");
        }

        /// <summary>
        /// A browser build for itch.io or GitHub Pages. Needs the Web Build Support
        /// module. Gzip with the decompression fallback means the page works on any
        /// static host without server configuration; upload the whole Builds/Web folder.
        /// </summary>
        [MenuItem("Head Soccer/Build Web (browser)", priority = 42)]
        public static void BuildWeb()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            Build(BuildTarget.WebGL, "Builds/Web");
        }

        private static void Build(BuildTarget target, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Builds");

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Head Soccer: {target} build succeeded, {summary.totalSize / (1024 * 1024)} MB at {path}");
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                Debug.LogError($"Head Soccer: {target} build {summary.result} with {summary.totalErrors} errors.");
            }
        }
    }
}
