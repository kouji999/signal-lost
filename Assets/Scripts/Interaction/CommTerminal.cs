using System.Collections;
using SignalLost.Core;
using SignalLost.Inventory;
using SignalLost.Narrative;
using UnityEngine;
using Inventory = SignalLost.Inventory.Inventory;

namespace SignalLost.Interaction
{
    public class CommTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private DoorController commDoor;
        [SerializeField] private float repairDuration = 3f;
        [SerializeField] private int requiredAccessLevel = 1;

        public bool Restored { get; private set; }
        public string Prompt => Restored ? "COMMUNICATION ARRAY: ONLINE"
            : Restoring ? "REPAIRING..." : "[E] REPAIR COMMUNICATION ARRAY";

        private bool Restoring;

        public bool CanInteract(GameObject interactor)
        {
            if (Restored || Restoring) return false;
            var inv = interactor.GetComponent<SignalLost.Inventory.Inventory>();
            if (inv == null || inv.HighestAccessLevel() < requiredAccessLevel)
            {
                EventBus.Publish(new SubtitleEvent("SYSTEM", "ACCESS LEVEL 1 REQUIRED", 2.5f));
                return false;
            }
            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            StartCoroutine(RepairRoutine());
        }

        private IEnumerator RepairRoutine()
        {
            Restoring = true;
            EventBus.Publish(new SubtitleEvent("SYSTEM", "REALIGNING COMMUNICATION ARRAY...", repairDuration));
            yield return new WaitForSeconds(repairDuration);

            Restored = true;
            Restoring = false;
            StoryFlagSystem.Set(StoryFlagKeys.CommunicationRestored);
            EventBus.Publish(new SignalReceived());
            EventBus.Publish(new SubtitleEvent("A.R.I.A.", "Communication array restored. Detecting unknown signal.", 4.5f));

            yield return new WaitForSeconds(5f);
            EventBus.Publish(new SubtitleEvent("UNKNOWN VOICE", "...is anyone there?", 3.5f));

            yield return new WaitForSeconds(4.5f);
            EventBus.Publish(new SubtitleEvent("UNKNOWN VOICE", "Don't trust the station.", 3.5f));

            yield return new WaitForSeconds(4f);
            EventBus.Publish(new SubtitleEvent("SYSTEM", "SIGNAL LOST", 3f));

            ObjectiveSystem.Instance.CompleteObjective("obj_communication");
            ObjectiveSystem.Instance.AddObjective("obj_truth", "Investigate The Signal",
                "The signal came from INSIDE the station. Find a LEVEL-2 keycard in the SECURITY office (east), then descend via the elevator.");

            if (commDoor != null) commDoor.SetOpen(true, broadcast: true);
        }
    }
}
