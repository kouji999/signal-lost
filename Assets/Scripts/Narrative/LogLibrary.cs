using System.Collections.Generic;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Narrative
{
    public class LogLibrary : MonoBehaviour
    {
        public static LogLibrary Instance { get; private set; }

        private readonly HashSet<string> _discovered = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public bool Discover(string logId)
        {
            if (!_discovered.Add(logId)) return false;
            StoryFlagSystem.Set("log:" + logId);
            EventBus.Publish(new LogDiscovered(logId));
            return true;
        }

        public bool IsDiscovered(string logId) => _discovered.Contains(logId);

        public IReadOnlyCollection<string> Discovered => _discovered;

        public void LoadFrom(IEnumerable<string> ids)
        {
            _discovered.Clear();
            foreach (var id in ids) _discovered.Add(id);
        }
    }
}
