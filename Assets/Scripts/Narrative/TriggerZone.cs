using System;
using UnityEngine;

namespace SignalLost.Narrative
{
    public class TriggerZone : MonoBehaviour
    {
        [SerializeField] private string tagFilter = "Player";
        public event Action<GameObject> Entered;

        private void OnTriggerEnter(Collider other)
        {
            if (!string.IsNullOrEmpty(tagFilter) && !other.CompareTag(tagFilter)) return;
            Entered?.Invoke(other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject);
        }
    }
}
