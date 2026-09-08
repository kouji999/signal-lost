using UnityEngine;

namespace SignalLost.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}
