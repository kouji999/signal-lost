using UnityEditor;
using UnityEngine;

namespace SignalLost.EditorTools
{
    public static class PlayerBuild
    {
        [MenuItem("SignalLost/Build Windows Player")]
        public static void BuildWindows()
        {
            BuildPlayer();
        }

        public static void BuildPlayer()
        {
            var scenes = new[] { new EditorBuildSettingsScene("Assets/_Project/Scenes/Main.unity", true) };
            EditorBuildSettings.scenes = scenes;

            var options = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(scenes, s => s.path),
                locationPathName = "Build/SignalLost/SignalLost.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[PlayerBuild] result=Succeeded size={summary.totalSize / (1024 * 1024)}MB errors={summary.totalErrors}");
            else
                Debug.LogError($"[PlayerBuild] result={summary.result} errors={summary.totalErrors}");
        }
    }
}
