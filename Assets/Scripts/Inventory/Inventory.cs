using System;
using System.Collections.Generic;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Inventory
{
    public class Inventory : MonoBehaviour
    {
        public const int MaxSlots = 6;

        private sealed class Slot
        {
            public ItemDefinition Def;
            public int Count;
        }

        private readonly List<Slot> _slots = new();

        public IReadOnlyList<ItemDefinition> Items
        {
            get
            {
                _view.Clear();
                foreach (var s in _slots) _view.Add(s.Def);
                return _view;
            }
        }
        private readonly List<ItemDefinition> _view = new();

        public int SlotCount => _slots.Count;
        public bool HasFreeSlot => _slots.Count < MaxSlots;

        public bool Add(ItemDefinition item)
        {
            if (item == null) return false;
            if (item.stackable)
            {
                foreach (var s in _slots)
                {
                    if (s.Def.itemId == item.itemId && s.Count < Mathf.Max(1, s.Def.maxStack))
                    {
                        s.Count++;
                        s.Def = item;
                        EventBus.Publish(InventoryChanged.Instance);
                        return true;
                    }
                }
            }
            if (!HasFreeSlot) return false;
            _slots.Add(new Slot { Def = item, Count = item.stackable ? 1 : 1 });
            EventBus.Publish(InventoryChanged.Instance);
            return true;
        }

        public bool Remove(ItemDefinition item)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Def.itemId != item.itemId) continue;
                _slots[i].Count--;
                if (_slots[i].Count <= 0) _slots.RemoveAt(i);
                EventBus.Publish(InventoryChanged.Instance);
                return true;
            }
            return false;
        }

        public ItemDefinition Find(string itemId)
        {
            foreach (var s in _slots) if (s.Def.itemId == itemId) return s.Def;
            return null;
        }

        public int Count(string itemId)
        {
            int n = 0;
            foreach (var s in _slots) if (s.Def.itemId == itemId) n += s.Count;
            return n;
        }

        public bool Has(string itemId) => Count(itemId) > 0;

        public bool Consume(string itemId, int amount = 1)
        {
            while (amount-- > 0)
            {
                var def = Find(itemId);
                if (def == null || !Remove(def)) return false;
            }
            return true;
        }

        public void UseSlot(int index)
        {
            if (index < 0 || index >= _slots.Count) return;
            var slot = _slots[index];
            if (slot.Def.kind == ItemKind.Keycard) return;
            ApplyEffects(slot.Def);
            slot.Count--;
            if (slot.Count <= 0) _slots.RemoveAt(index);
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

        public List<(ItemDefinition def, int count)> SlotInfos()
        {
            var list = new List<(ItemDefinition, int)>();
            foreach (var s in _slots) list.Add((s.Def, s.Count));
            return list;
        }

        public int HighestAccessLevel()
        {
            int level = 0;
            foreach (var s in _slots)
                if (s.Def.kind == ItemKind.Keycard && s.Def.accessLevel > level)
                    level = s.Def.accessLevel;
            return level;
        }

        public List<string> SnapshotIds()
        {
            var list = new List<string>();
            foreach (var s in _slots)
                for (int i = 0; i < s.Count; i++) list.Add(s.Def.itemId);
            return list;
        }

        public void RestoreFromIds(List<string> ids, Func<string, ItemDefinition> resolver)
        {
            _slots.Clear();
            foreach (var id in ids)
            {
                var item = resolver(id);
                if (item == null) continue;
                if (AddSilent(item)) continue;
            }
            EventBus.Publish(InventoryChanged.Instance);
        }

        private bool AddSilent(ItemDefinition item)
        {
            if (item.stackable)
            {
                foreach (var s in _slots)
                    if (s.Def.itemId == item.itemId && s.Count < Mathf.Max(1, s.Def.maxStack))
                    {
                        s.Count++;
                        return true;
                    }
            }
            if (!HasFreeSlot) return false;
            _slots.Add(new Slot { Def = item, Count = 1 });
            return true;
        }
    }
}
