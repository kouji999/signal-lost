using System.Collections;
using SignalLost.Core;
using SignalLost.Interaction;
using UnityEngine;

namespace SignalLost.Narrative
{
    public class DoorAnomalyEvent : MonoBehaviour
    {
        [SerializeField] private DoorController targetDoor;
        [SerializeField] private TriggerZone trigger;
        [SerializeField] private float delayAfterLifeSupport = 6f;
        [SerializeField] private float autoCloseDelay = 10f;

        private bool _done;

        private void OnEnable()
        {
            EventBus.Subscribe<LifeSupportRestored>(OnLifeSupportRestored);
            if (trigger != null) trigger.Entered += OnTriggerEntered;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LifeSupportRestored>(OnLifeSupportRestored);
            if (trigger != null) trigger.Entered -= OnTriggerEntered;
        }

        private void OnLifeSupportRestored(LifeSupportRestored evt)
        {
            if (_done) return;
            StartCoroutine(AnomalyRoutine());
        }

        private void OnTriggerEntered(GameObject go) { }

        private IEnumerator AnomalyRoutine()
        {
            yield return new WaitForSeconds(delayAfterLifeSupport);
            if (targetDoor != null) targetDoor.SetOpen(true);
            StoryFlagSystem.Set(StoryFlagKeys.DoorAnomaly);

            yield return new WaitForSeconds(autoCloseDelay);
            if (targetDoor != null && !targetDoor.IsOpen) yield break;
            if (targetDoor != null) targetDoor.SetOpen(false);
        }
    }
}
