using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Narrative
{
    public class TerminalChoice : MonoBehaviour, Interaction.IInteractable
    {
        public int kind;

        public string Prompt => kind switch
        {
            0 => "[E] SHUT DOWN A.R.I.A.",
            1 => "[E] STAY - REMAIN DIGITAL",
            _ => "[E] TRANSMIT - SEND ME OUT"
        };

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            var mgr = FindFirstObjectByType<EndingManager>();
            if (mgr != null) mgr.Begin(kind);
        }
    }
}
