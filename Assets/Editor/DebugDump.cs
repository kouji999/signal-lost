using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SignalLost.EditorTools
{
    public static class DebugDump
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity", OpenSceneMode.Single);
            var db = Object.FindAnyObjectByType<Items.ItemDatabase>();
            if (db == null) { Debug.Log("[Dump] ItemDatabase NOT FOUND"); return; }
            var so = new SerializedObject(db);
            var items = so.FindProperty("items");
            Debug.Log($"[Dump] ItemDatabase.items.count = {items.arraySize}");
            for (int i = 0; i < items.arraySize; i++)
                Debug.Log($"[Dump] item[{i}] = {items.GetArrayElementAtIndex(i).objectReferenceValue}");
            var cell = AssetDatabase.LoadAssetAtPath<Inventory.ItemDefinition>("Assets/_Project/Resources/Items/power_cell.asset");
            Debug.Log($"[Dump] power_cell asset = {(cell != null ? "OK" : "NULL")}");
            foreach (var p in Object.FindObjectsByType<Interaction.PickupItem>())
                Debug.Log($"[Dump] pickup {p.PickupId} item={(p.GetComponent<Interaction.PickupItem>() != null ? "comp" : "?")}");
            var so2 = new SerializedObject(Object.FindAnyObjectByType<Interaction.PickupItem>());
            Debug.Log($"[Dump] first pickup item ref = {so2.FindProperty("item").objectReferenceValue}");
            var lst = Object.FindAnyObjectByType<Interaction.LifeSupportTerminal>();
            var so3 = new SerializedObject(lst);
            Debug.Log($"[Dump] LS terminal powerCellItem = {so3.FindProperty("powerCellItem").objectReferenceValue}");
            Debug.Log("[Dump] DONE");
        }
    }
}
