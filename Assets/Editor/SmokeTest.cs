using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SignalLost.EditorTools
{
    public static class SmokeTest
    {
        [MenuItem("SignalLost/Run Smoke Test")]
        public static void RunMenu() => Run();

        public static void Run()
        {
            int failures = 0;

            if (!EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity", OpenSceneMode.Single).IsValid())
            {
                Debug.LogError("[SmokeTest] Cannot open Main.unity");
                return;
            }

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
            Check("security room", GameObject.Find("SecurityRoom") != null);
            Check("cctv terminal", GameObject.Find("CCTVTerminal") != null);
            Check("keycard2 pickup", GameObject.Find("Pickup_Keycard2") != null);
            Check("elevator door", GameObject.Find("door_elevator") != null);
            Check("research lab", GameObject.Find("ResearchLab") != null);
            Check("shiren pod", GameObject.Find("ShirenPod") != null);
            Check("core room", GameObject.Find("CoreRoom") != null);
            Check("aria pillar", GameObject.Find("ARIAPillar") != null);
            Check("ending terminals", GameObject.Find("EndingTerminal_A") != null && GameObject.Find("EndingTerminal_B") != null && GameObject.Find("EndingTerminal_C") != null);
            Check("ending manager", UnityEngine.Object.FindAnyObjectByType<SignalLost.Narrative.EndingManager>() != null);
            Check("hunter", GameObject.Find("Hunter") != null);
            var ghostFound = false;
            foreach (var x in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (x != null && x.name == "ObserverGhost") { ghostFound = true; break; }
            Check("observer ghost", ghostFound);
            Check("door_hub", GameObject.Find("door_hub") != null);
            Check("door_pod", GameObject.Find("door_pod") != null);
            Check("door_hub", GameObject.Find("door_hub") != null);
            Check("door_medbay", GameObject.Find("door_medbay") != null);
            Check("door_storage", GameObject.Find("door_storage") != null);
            Check("door_comm", GameObject.Find("door_comm") != null);
            Check("medbay room", GameObject.Find("Medbay") != null);
            Check("storage room", GameObject.Find("Storage") != null);
            Check("comm room", GameObject.Find("CommRoom") != null);
            Check("power cell pickup", GameObject.Find("Pickup_PowerCell") != null);
            Check("keycard pickup", GameObject.Find("Pickup_Keycard") != null);
            Check("hud canvas", GameObject.Find("HUD_Canvas") != null);
            Check("event system", GameObject.Find("EventSystem") != null);
            Check("systems root", GameObject.Find("Systems") != null);
            Check("door layer defined (nav runtime-ready)", LayerMask.NameToLayer("Door") >= 0);
            Check("item assets", AssetDatabase.LoadAssetAtPath<SignalLost.Inventory.ItemDefinition>("Assets/_Project/ScriptableObjects/Items/power_cell.asset") != null);
            Check("log asset", AssetDatabase.LoadAssetAtPath<SignalLost.Narrative.AudioLogDefinition>("Assets/_Project/ScriptableObjects/log_018.asset") != null);
            Check("urp asset", GraphicsSettings.defaultRenderPipeline != null);

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Check("player controller", player.GetComponent<SignalLost.Player.PlayerController>() != null);
                Check("player vitals", player.GetComponent<SignalLost.Player.PlayerVitals>() != null);
                Check("player inventory", player.GetComponent<SignalLost.Inventory.Inventory>() != null);
            }

            if (failures == 0)
                Debug.Log("[SmokeTest] ALL SMOKE TESTS PASSED");
            else
                Debug.LogError($"[SmokeTest] {failures} FAILURES");
        }
    }
}
