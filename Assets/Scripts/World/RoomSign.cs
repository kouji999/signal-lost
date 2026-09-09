using SignalLost.Core;
using UnityEngine;

namespace SignalLost.World
{
    public class RoomSign : MonoBehaviour
    {
        [SerializeField] private string areaName = "UNKNOWN SECTOR";
        [SerializeField] private float cooldown = 25f;

        private float _last;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (Time.time - _last < cooldown) return;
            _last = Time.time;
            EventBus.Publish(new SubtitleEvent("STATION", $"> ENTERING {areaName}", 2.5f));
        }
    }
}
