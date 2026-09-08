using System.Collections.Generic;
using SignalLost.Inventory;
using UnityEngine;

namespace SignalLost.Items
{
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        [SerializeField] private List<ItemDefinition> items = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public ItemDefinition Resolve(string itemId) => items.Find(i => i != null && i.itemId == itemId);
    }
}
