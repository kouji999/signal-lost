using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Interaction
{
    public class PickupItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string pickupId = "pickup_01";
        [SerializeField] private Inventory.ItemDefinition item;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioSource audioSource;

        public string PickupId => pickupId;
        public bool Collected { get; private set; }

        public string Prompt => item != null ? $"[E] TAKE {item.displayName.ToUpper()}" : "[E] TAKE";

        public bool CanInteract(GameObject interactor) => !Collected;

        public void Interact(GameObject interactor)
        {
            if (Collected || item == null) return;
            var inv = interactor.GetComponent<Inventory.Inventory>();
            if (inv == null || !inv.Add(item))
            {
                EventBus.Publish(new SubtitleEvent("SYSTEM", "INVENTORY FULL", 2f));
                return;
            }

            Collected = true;
            if (audioSource != null && pickupSound != null) audioSource.PlayOneShot(pickupSound);
            EventBus.Publish(new SubtitleEvent("SYSTEM", $"ACQUIRED: {item.displayName.ToUpper()}", 2.2f));
            StoryFlagSystem.Set("pickup:" + pickupId);
            gameObject.SetActive(false);
        }

        public void ForceCollect()
        {
            Collected = true;
            gameObject.SetActive(false);
        }
    }
}
