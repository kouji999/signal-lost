using System.Collections.Generic;
using UnityEngine;

namespace SignalLost.Narrative
{
    [System.Serializable]
    public class LogEntry
    {
        public string logId;
        public string title;
        [TextArea] public string content;
    }

    [CreateAssetMenu(fileName = "Log_", menuName = "SignalLost/Audio Log")]
    public class AudioLogDefinition : ScriptableObject
    {
        public string logId;
        public string title;
        [TextArea(3, 10)] public string content;
        public AudioClip clip;
    }
}
