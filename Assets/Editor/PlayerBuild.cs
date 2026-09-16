using System.IO;
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
            PlayerSettings.companyName = "Raliq Studio";
            PlayerSettings.productName = "SIGNAL LOST";
            PlayerSettings.bundleVersion = "0.1.0-preview";
            PlayerSettings.runInBackground = false;
            TryApplyIcon();

            var scenes = new[] { new EditorBuildSettingsScene("Assets/_Project/Scenes/Main.unity", true) };
            EditorBuildSettings.scenes = scenes;

            var target = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/SignalLost/SignalLost.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(target));

            var options = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(scenes, s => s.path),
                locationPathName = target,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[PlayerBuild] result=Succeeded size={summary.totalSize / (1024 * 1024)}MB errors={summary.totalErrors} path={target}");
            else
                Debug.LogError($"[PlayerBuild] result={summary.result} errors={summary.totalErrors}");
        }

        static void TryApplyIcon()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/UI/app_icon.png");
            if (tex == null)
                Debug.LogWarning("[PlayerBuild] app_icon.png missing - assign icon manually in Player Settings");
            else
                Debug.Log("[PlayerBuild] icon file present (Assets/_Project/UI/app_icon.png) - assign once via Player Settings > Icons > Windows");
        }
    }
}
