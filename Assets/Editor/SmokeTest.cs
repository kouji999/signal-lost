using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SignalLost.EditorTools
{
    public static class SmokeTest
    {
        [MenuItem("SignalLost/Run Smoke Test")]
        public static void RunMenu() => Run();

        public static void Run()
        {
            int failures = 0;

            void Check(string label, bool ok)
            {
                if (ok) Debug.Log($"[SmokeTest] PASS {label}");
                else { failures++; Debug.LogError($"[SmokeTest] FAIL {label}"); }
            }

            Check("scene loaded", EditorSceneManager.GetActiveScene().IsValid());
            Check("player exists", GameObject.FindGameObjectWithTag("Player") != null);
            Check("echo exists", GameObject.Find("Echo") != null);
            Check("ls terminal", GameObject.Find("LifeSupportTerminal") != null);
            Check("comm terminal", GameObject.Find("CommTerminal") != null);
            Check("log terminal", GameObject.Find("LogTerminal018") != null);
            Check("door_pod", GameObject.Find("door_pod") != null);
            Check("door_hub", GameObject.Find("door_hub") != null);
            Check("door_ls", GameObject.Find("door_ls") != null);
            Check("door_anomaly", GameObject.Find("door_anomaly") != null);
            Check("door_comm", GameObject.Find("door_comm") != null);
            Check("power cell pickup", GameObject.Find("Pickup_PowerCell") != null);
            Check("keycard pickup", GameObject.Find("Pickup_Keycard") != null);
            Check("hud canvas", GameObject.Find("HUD_Canvas") != null);
            Check("event system", GameObject.Find("EventSystem") != null);
            Check("systems root", GameObject.Find("Systems") != null);
            Check("item assets", AssetDatabase.LoadAssetAtPath<SignalLost.Inventory.ItemDefinition>("Assets/_Project/ScriptableObjects/Items/power_cell.asset") != null);
            Check("log asset", AssetDatabase.LoadAssetAtPath<SignalLost.Narrative.AudioLogDefinition>("Assets/_Project/ScriptableObjects/log_018.asset") != null);
            Check("urp asset", GraphicsSettings.defaultRenderPipeline != null);

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Check("player controller", player.GetComponent<SignalLost.Player.PlayerController>() != null);
                Check("player vitals", player.GetComponent<SignalLost.Player.PlayerVitals>() != null);
                Check("player inventory", player.GetComponent<SignalLost.Inventory.Inventory>() != null);
                Check("navmesh on player path", UnityEngine.AI.NavMesh.SamplePosition(player.transform.position, out _, 3f, UnityEngine.AI.NavMesh.AllAreas));
            }

            var echo = GameObject.Find("Echo");
            if (echo != null)
                Check("echo on navmesh", UnityEngine.AI.NavMesh.SamplePosition(echo.transform.position, out _, 3f, UnityEngine.AI.NavMesh.AllAreas));

            if (failures == 0)
                Debug.Log("[SmokeTest] ALL SMOKE TESTS PASSED");
            else
                Debug.LogError($"[SmokeTest] {failures} FAILURES");
        }
    }
}
