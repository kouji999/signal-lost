using System.IO;
using UnityEditor;
using UnityEngine;

namespace SignalLost.EditorTools
{
    [InitializeOnLoad]
    public static class CommandFileWatcher
    {
        const string TriggerPath = "Library/command_trigger.txt";
        const string StatusPath = "Library/command_status.txt";

        static CommandFileWatcher()
        {
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (!File.Exists(TriggerPath)) return;
            string cmd;
            try { cmd = File.ReadAllText(TriggerPath).Trim(); }
            catch { return; }

            try { File.Delete(TriggerPath); } catch { }

            var result = "unknown";
            switch (cmd)
            {
                case "build":
                    result = RunBuild();
                    break;
                case "ping":
                    result = "pong";
                    break;
                case "fixurp":
                    FixUrp();
                    result = "fixurp_done";
                    break;
                case "config":
                    ConfigBuild();
                    result = "config_done";
                    break;
                case "restart":
                    result = "bye";
                    EditorApplication.delayCall += () => EditorApplication.Exit(0);
                    break;
            }

            try { File.WriteAllText(StatusPath, result); } catch { }
            Debug.Log($"[CommandWatcher] {cmd} -> {result}");
        }

        static string RunBuild()
        {
            SceneBuilder.BuildMain();
            PlayerBuild.BuildPlayer();
            return "build_done";
        }

        static void FixUrp()
        {
            SceneBuilder.FixUrpRenderer();
        }

        static void ConfigBuild()
        {
            Debug.Log("[ConfigBuild] no-op placeholder");
        }
    }
}
