using System;
using System.Collections.Generic;

namespace SignalLost.Save
{
    [Serializable]
    public class SaveData
    {
        public string Version = "mvp1";
        public float PlayerX, PlayerY, PlayerZ, PlayerYaw;
        public float Health, Oxygen, Battery;
        public bool FlashlightOn;
        public string GameState;
        public List<string> Inventory = new();
        public List<string> StoryFlags = new();
        public List<string> DiscoveredLogs = new();
        public List<string> CollectedPickups = new();
        public List<DoorSave> Doors = new();
        public bool LifeSupportOnline;
        public string Timestamp;
    }

    [Serializable]
    public class DoorSave
    {
        public string DoorId;
        public bool Open;
    }
}
