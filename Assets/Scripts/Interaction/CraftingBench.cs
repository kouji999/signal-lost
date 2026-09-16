using System.Collections.Generic;
using System.Text;
using SignalLost.Core;
using SignalLost.Inventory;
using SignalLost.Items;
using UnityEngine;

namespace SignalLost.Interaction
{
    [System.Serializable]
    public class CraftRecipe
    {
        public string outputItemId;
        public string inputA;
        public int countA = 1;
        public string inputB = "";
        public int countB = 1;
    }

    public class CraftingBench : MonoBehaviour, IInteractable
    {
        [SerializeField] private List<CraftRecipe> recipes = new();
        [SerializeField] private string openLine = "FIELD WORKBENCH — [1]-[3] CRAFT   [E] CLOSE";

        public bool Active { get; private set; }

        private SignalLost.Inventory.Inventory _inv;

        public string Prompt => Active ? "[E] CLOSE WORKBENCH" : "[E] USE FIELD WORKBENCH";

        public bool CanInteract(GameObject interactor) =>
            GameManager.Instance != null &&
            (GameManager.Instance.State == GameState.Playing || Active);

        public void Interact(GameObject interactor)
        {
            _inv = interactor.GetComponent<SignalLost.Inventory.Inventory>();
            Toggle();
        }

        public void Toggle()
        {
            Active = !Active;
            if (Active)
            {
                if (GameManager.Instance.State == GameState.Playing)
                    GameManager.Instance.SetState(GameState.UiOpen);
                RefreshHud();
            }
            else if (GameManager.Instance.State == GameState.UiOpen)
            {
                GameManager.Instance.SetState(GameState.Playing);
                EventBus.Publish(new ScannerResult(null));
            }
        }

        private void Update()
        {
            if (!Active || GameManager.Instance.State != GameState.UiOpen) return;
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))
            {
                Toggle();
                return;
            }
            for (int i = 0; i < recipes.Count && i < 4; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    Craft(i);
        }

        public bool Craft(int index)
        {
            if (_inv == null)
                _inv = GameObject.FindGameObjectWithTag("Player")?.GetComponent<SignalLost.Inventory.Inventory>();
            if (_inv == null || index < 0 || index >= recipes.Count) return false;
            var r = recipes[index];
            var outDef = Resolve(r.outputItemId);
            if (outDef == null) return false;
            if (!CanCraft(r)) return false;

            _inv.Consume(r.inputA, r.countA);
            if (!string.IsNullOrEmpty(r.inputB)) _inv.Consume(r.inputB, r.countB);
            _inv.Add(outDef);
            EventBus.Publish(new SubtitleEvent("SYSTEM", $"FABRICATED: {outDef.displayName.ToUpper()}", 2.5f));
            RefreshHud();
            return true;
        }

        public bool CanCraft(CraftRecipe r)
        {
            if (_inv == null)
                _inv = GameObject.FindGameObjectWithTag("Player")?.GetComponent<SignalLost.Inventory.Inventory>();
            if (_inv == null) return false;
            if (_inv.Count(r.inputA) < r.countA) return false;
            if (!string.IsNullOrEmpty(r.inputB) && _inv.Count(r.inputB) < r.countB) return false;
            return true;
        }

        private ItemDefinition Resolve(string itemId)
        {
            if (ItemDatabase.Instance == null) return null;
            return ItemDatabase.Instance.Resolve(itemId);
        }

        private void RefreshHud()
        {
            var sb = new StringBuilder();
            sb.AppendLine(openLine.ToUpper());
            for (int i = 0; i < recipes.Count; i++)
            {
                var r = recipes[i];
                var outDef = Resolve(r.outputItemId);
                string name = outDef != null ? outDef.displayName.ToUpper() : r.outputItemId.ToUpper();
                string cost = $"{r.countA} {Name(r.inputA)}";
                if (!string.IsNullOrEmpty(r.inputB)) cost += $" + {r.countB} {Name(r.inputB)}";
                string mark = CanCraft(r) ? "<color=#7fd1a0>READY</color>" : "<color=#8d949d>need mats</color>";
                sb.AppendLine($"[{i + 1}] {name}  ←  {cost}   {mark}");
            }
            EventBus.Publish(new ScannerResult(sb.ToString()));
        }

        private string Name(string itemId)
        {
            var d = Resolve(itemId);
            return d != null ? d.displayName.ToUpper() : itemId.ToUpper();
        }
    }
}
