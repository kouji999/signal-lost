using System.Collections.Generic;
using SignalLost.Core;
using SignalLost.Interaction;
using SignalLost.Narrative;
using UnityEngine;

namespace SignalLost.Save
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private static readonly string SaveFilePath =
            System.IO.Path.Combine(Application.persistentDataPath, "signal_lost_save.json");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LifeSupportRestored>(OnAutoSavePoint);
            EventBus.Subscribe<SliceCompleteEvent>(OnAutoSavePoint);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LifeSupportRestored>(OnAutoSavePoint);
            EventBus.Unsubscribe<SliceCompleteEvent>(OnAutoSavePoint);
        }

        private void OnAutoSavePoint<T>(T evt) => Save();

        public void Save()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            var data = new SaveData
            {
                Timestamp = System.DateTime.UtcNow.ToString("o"),
                GameState = GameManager.Instance != null ? GameManager.Instance.State.ToString() : "Playing"
            };

            if (player != null)
            {
                data.PlayerX = player.transform.position.x;
                data.PlayerY = player.transform.position.y;
                data.PlayerZ = player.transform.position.z;
                data.PlayerYaw = player.transform.eulerAngles.y;

                var vitals = player.GetComponent<SignalLost.Player.PlayerVitals>();
                if (vitals != null)
                {
                    data.Health = vitals.Health;
                    data.Oxygen = vitals.Oxygen;
                    data.Battery = vitals.Battery;
                }

                var inv = player.GetComponent<SignalLost.Inventory.Inventory>();
                if (inv != null) data.Inventory = inv.SnapshotIds();

                var flash = player.GetComponentInChildren<SignalLost.Player.Flashlight>();
                if (flash != null) data.FlashlightOn = flash.IsOn;
            }

            data.StoryFlags = new List<string>(StoryFlagSystem.AllFlags);

            if (LogLibrary.Instance != null)
                data.DiscoveredLogs = new List<string>(LogLibrary.Instance.Discovered);

            foreach (var p in FindObjectsByType<PickupItem>(FindObjectsSortMode.None))
                if (p.Collected) data.CollectedPickups.Add(p.PickupId);

            foreach (var d in DoorRegistry.All)
                data.Doors.Add(new DoorSave { DoorId = d.DoorId, Open = d.IsOpen });

            var terminal = FindFirstObjectByType<LifeSupportTerminal>();
            if (terminal != null) data.LifeSupportOnline = terminal.LifeSupportOnline;

            System.IO.File.WriteAllText(SaveFilePath, JsonUtility.ToJson(data, true));
            EventBus.Publish(new SubtitleEvent("SYSTEM", "PROGRESS SAVED", 1.5f));
        }

        public bool HasSave() => System.IO.File.Exists(SaveFilePath);

        public void LoadAndApply()
        {
            if (!HasSave())
            {
                EventBus.Publish(new SubtitleEvent("SYSTEM", "NO SAVE FOUND", 2f));
                return;
            }

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(System.IO.File.ReadAllText(SaveFilePath));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Load failed: {e.Message}");
                return;
            }

            StoryFlagSystem.LoadFrom(data.StoryFlags);
            if (LogLibrary.Instance != null) LogLibrary.Instance.LoadFrom(data.DiscoveredLogs);

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(data.PlayerX, data.PlayerY, data.PlayerZ);
                player.transform.rotation = Quaternion.Euler(0f, data.PlayerYaw, 0f);

                var vitals = player.GetComponent<SignalLost.Player.PlayerVitals>();
                if (vitals != null) vitals.Restore(data.Health, data.Oxygen, data.Battery);

                var inv = player.GetComponent<SignalLost.Inventory.Inventory>();
                if (inv != null && Items.ItemDatabase.Instance != null)
                    inv.RestoreFromIds(data.Inventory, Items.ItemDatabase.Instance.Resolve);

                var flash = player.GetComponentInChildren<SignalLost.Player.Flashlight>();
                if (flash != null) flash.SetOn(data.FlashlightOn);
            }

            foreach (var p in FindObjectsByType<PickupItem>(FindObjectsSortMode.None))
                if (data.CollectedPickups.Contains(p.PickupId)) p.ForceCollect();

            foreach (var d in data.Doors)
            {
                var door = DoorRegistry.Find(d.DoorId);
                if (door != null) door.SetOpen(d.Open);
            }

            var terminal = FindFirstObjectByType<LifeSupportTerminal>();
            if (terminal != null && data.LifeSupportOnline) terminal.RestoreOnline();

            EventBus.Publish(new SubtitleEvent("SYSTEM", "PROGRESS RESTORED", 2f));
        }

        public void DeleteSave()
        {
            if (HasSave()) System.IO.File.Delete(SaveFilePath);
        }
    }
}
