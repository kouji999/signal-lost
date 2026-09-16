using SignalLost.Core;
using SignalLost.Inventory;
using UnityEngine;

namespace SignalLost.Interaction
{
    public class PowerNode : MonoBehaviour, IInteractable
    {
        [SerializeField] private string nodeId = "power_node";
        [SerializeField] private string fuseItemId = "fuse";
        [SerializeField] private int fusesRequired = 2;
        [SerializeField] private string restoresFlag = "engineering_power";
        [SerializeField] private Light statusLight;
        [SerializeField] private float restoreDuration = 2.5f;

        public bool Inserted { get; private set; }
        public bool Online { get; private set; }
        public string Prompt => Online ? "POWER NODE: ONLINE"
            : Inserted ? "[E] ENGAGE POWER ROUTING"
            : $"[E] POWER NODE — NEEDS {fusesRequired}x FUSE";

        private bool _busy;

        private void Awake()
        {
            if (statusLight != null) statusLight.enabled = false;
        }

        public bool CanInteract(GameObject interactor) => !Online && !_busy;

        public void Interact(GameObject interactor)
        {
            if (Online || _busy) return;
            var inv = interactor.GetComponent<SignalLost.Inventory.Inventory>();

            if (!Inserted)
            {
                if (inv == null) return;
                if (inv.Count(fuseItemId) < fusesRequired)
                {
                    int have = inv != null ? inv.Count(fuseItemId) : 0;
                    EventBus.Publish(new SubtitleEvent("SYSTEM",
                        $"POWER NODE OFFLINE — requires {fusesRequired} fuses (carrying {have}). [Q] SCANNER traces them.", 4.5f));
                    return;
                }
                inv.Consume(fuseItemId, fusesRequired);
                Inserted = true;
                EventBus.Publish(new SubtitleEvent("SYSTEM", "FUSES INSERTED — routing circuit engaged", 3f));
                return;
            }

            _busy = true;
            StartCoroutine(Engage());
        }

        private System.Collections.IEnumerator Engage()
        {
            EventBus.Publish(new SubtitleEvent("SYSTEM", "ROUTING POWER...", restoreDuration));
            yield return new WaitForSeconds(restoreDuration);
            Online = true;
            _busy = false;
            if (statusLight != null) statusLight.enabled = true;
            StoryFlagSystem.Set(restoresFlag);
            EventBus.Publish(PowerRestored.Instance);
            EventBus.Publish(new SubtitleEvent("A.R.I.A.", "Power rerouted. Engineering decks are back online. Thank you for maintaining my body.", 5f));
        }
    }
}
