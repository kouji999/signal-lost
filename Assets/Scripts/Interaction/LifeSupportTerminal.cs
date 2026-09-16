using System.Collections;
using SignalLost.Core;
using SignalLost.Inventory;
using SignalLost.Narrative;
using UnityEngine;
using Inventory = SignalLost.Inventory.Inventory;

namespace SignalLost.Interaction
{
    public class LifeSupportTerminal : MonoBehaviour, IInteractable
    {
        [Header("Puzzle stages")]
        [SerializeField] private string powerCellItemId = "power_cell";
        [SerializeField] private ItemDefinition powerCellItem;
        [SerializeField] private PowerCellSlot slot;
        [SerializeField] private float restartDuration = 4f;
        [SerializeField] private Light statusLight;
        [SerializeField] private Color offColor = Color.red;
        [SerializeField] private Color onColor = Color.green;

        public bool LifeSupportOnline { get; private set; }
        public bool CellInserted => slot != null && slot.Filled;
        public string Prompt => GetPrompt();

        private string GetPrompt()
        {
            if (LifeSupportOnline) return "LIFE SUPPORT: ONLINE";
            if (CellInserted) return "[E] RESTART LIFE SUPPORT";
            return "OFFLINE — POWER CELL REQUIRED (STORAGE BAY, east)";
        }

        private void Awake()
        {
            if (statusLight != null) statusLight.color = offColor;
        }

        public bool CanInteract(GameObject interactor) => !LifeSupportOnline;

        public void Interact(GameObject interactor)
        {
            if (LifeSupportOnline) return;

            if (!CellInserted)
            {
                var inv = interactor.GetComponent<SignalLost.Inventory.Inventory>();
                if (inv == null) return;

                ItemDefinition cell = null;
                foreach (var it in inv.Items)
                {
                    if (it.itemId == powerCellItemId) { cell = it; break; }
                }
                if (cell == null)
                {
                    EventBus.Publish(new SubtitleEvent("A.R.I.A.", "The console needs a power cell. One was logged in the STORAGE BAY — the east door of this deck.", 5f));
                    return;
                }

                inv.Remove(cell);
                slot.Fill();
                EventBus.Publish(new SubtitleEvent("SYSTEM", "POWER CELL INSERTED", 2.5f));
                return;
            }

            StartCoroutine(RestartRoutine());
        }

        public void RestoreOnline()
        {
            LifeSupportOnline = true;
            if (statusLight != null) statusLight.color = onColor;
            StoryFlagSystem.Set(StoryFlagKeys.LifeSupportOnline);
            if (slot != null && !slot.Filled) slot.Fill();
            EventBus.Publish(new LifeSupportRestored());
        }

        private IEnumerator RestartRoutine()
        {
            EventBus.Publish(new SubtitleEvent("SYSTEM", "RESTARTING LIFE SUPPORT...", 3f));
            yield return new WaitForSeconds(restartDuration);

            LifeSupportOnline = true;
            if (statusLight != null) statusLight.color = onColor;
            StoryFlagSystem.Set(StoryFlagKeys.LifeSupportOnline);
            EventBus.Publish(new LifeSupportRestored());
            EventBus.Publish(new SubtitleEvent("A.R.I.A.", "Life support restored. Well done.", 4f));
        }
    }
}
