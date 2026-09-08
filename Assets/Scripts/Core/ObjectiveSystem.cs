using System;
using System.Collections.Generic;
using UnityEngine;

namespace SignalLost.Core
{
    [Serializable]
    public class Objective
    {
        public string Id;
        public string Title;
        public string Description;
        public bool Completed;
    }

    public class ObjectiveSystem : MonoBehaviour
    {
        public static ObjectiveSystem Instance { get; private set; }

        private readonly List<Objective> _active = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void AddObjective(string id, string title, string description)
        {
            if (_active.Exists(o => o.Id == id)) return;
            _active.Add(new Objective { Id = id, Title = title, Description = description });
            EventBus.Publish(ObjectiveChanged.Instance);
        }

        public void CompleteObjective(string id)
        {
            var obj = _active.Find(o => o.Id == id);
            if (obj == null || obj.Completed) return;
            obj.Completed = true;
            EventBus.Publish(ObjectiveChanged.Instance);
        }

        public void RemoveObjective(string id)
        {
            _active.RemoveAll(o => o.Id == id);
            EventBus.Publish(ObjectiveChanged.Instance);
        }

        public IReadOnlyList<Objective> ActiveObjectives => _active;
    }
}
