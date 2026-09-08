using System;
using System.Collections.Generic;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Inventory
{
    public class Inventory : MonoBehaviour
    {
        public const int MaxSlots = 6;

        private readonly List<ItemDefinition> _items = new();

        public IReadOnlyList<ItemDefinition> Items => _items;
        public bool HasFreeSlot => _items.Count < MaxSlots;

        public bool Add(ItemDefinition item)
        {
            if (item == null) return false;
            if (item.stackable)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].itemId == item.itemId && _items[i].stackable)
                    {
                        _items[i] = item;
                        EventBus.Publish(InventoryChanged.Instance);
                        return true;
                    }
                }
            }
            if (!HasFreeSlot) return false;
            _items.Add(item);
            EventBus.Publish(InventoryChanged.Instance);
            return true;
        }

        public bool Remove(ItemDefinition item)
        {
            if (!_items.Remove(item)) return false;
            EventBus.Publish(InventoryChanged.Instance);
            return true;
        }

        public ItemDefinition Find(string itemId) => _items.Find(i => i.itemId == itemId);

        public int Count(string itemId)
        {
            int n = 0;
            for (int i = 0; i < _items.Count; i++)
                if (_items[i].itemId == itemId) n++;
            return n;
        }

        public bool Has(string itemId) => Count(itemId) > 0;

        public bool Consume(string itemId)
        {
            var item = Find(itemId);
            if (item == null || !Remove(item)) return false;
            ApplyEffects(item);
            return true;
        }

        public void UseSlot(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            var item = _items[index];
            if (item.kind == ItemKind.Keycard) return;
            ApplyEffects(item);
            _items.RemoveAt(index);
            EventBus.Publish(InventoryChanged.Instance);
        }

        private void ApplyEffects(ItemDefinition item)
        {
            var vitals = GetComponent<Player.PlayerVitals>();
            if (vitals == null) return;
            if (item.healAmount > 0f) vitals.Heal(item.healAmount);
            if (item.oxygenAmount > 0f) vitals.RefillOxygen(item.oxygenAmount);
            if (item.batteryCharge > 0f) vitals.ChargeBattery(item.batteryCharge);
        }

        public int HighestAccessLevel()
        {
            int level = 0;
            for (int i = 0; i < _items.Count; i++)
                if (_items[i].kind == ItemKind.Keycard && _items[i].accessLevel > level)
                    level = _items[i].accessLevel;
            return level;
        }

        public List<string> SnapshotIds()
        {
            var list = new List<string>();
            foreach (var item in _items) list.Add(item.itemId);
            return list;
        }

        public void RestoreFromIds(List<string> ids, Func<string, ItemDefinition> resolver)
        {
            _items.Clear();
            foreach (var id in ids)
            {
                var item = resolver(id);
                if (item != null) _items.Add(item);
            }
            EventBus.Publish(InventoryChanged.Instance);
        }
    }
}
