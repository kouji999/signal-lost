using System.Collections.Generic;
using SignalLost.Inventory;
using UnityEngine;

namespace SignalLost.Items
{
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        [SerializeField] private List<ItemDefinition> items = new();

        public void Init(IEnumerable<ItemDefinition> defs)
        {
            items.Clear();
            foreach (var d in defs)
            {
                if (d != null) items.Add(d);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (items.Count == 0)
                items.AddRange(Resources.LoadAll<ItemDefinition>("Items"));
        }

        public ItemDefinition Resolve(string itemId) => items.Find(i => i != null && i.itemId == itemId);
    }
}
