using UnityEngine;

namespace SignalLost.Inventory
{
    public enum ItemKind
    {
        Consumable,
        Keycard,
        Component,
        Tool
    }

    [CreateAssetMenu(fileName = "Item_", menuName = "SignalLost/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public ItemKind kind = ItemKind.Consumable;
        public Color uiColor = Color.white;

        [Header("Consumable Effects")]
        public float healAmount;
        public float oxygenAmount;
        public float batteryCharge;

        [Header("Keycard")]
        public int accessLevel;

        [Header("Stacking")]
        public bool stackable = true;
        public int maxStack = 3;
    }
}
