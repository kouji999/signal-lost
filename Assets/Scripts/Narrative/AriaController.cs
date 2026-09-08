using System.Collections;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Narrative
{
    public class AriaController : MonoBehaviour
    {
        [Header("Chapter 1 lines")]
        [TextArea] public string emergencyLine = "Emergency protocol activated.";
        [TextArea] public string objectiveLine = "Objective: Restore Life Support.";
        [TextArea] public string lifeSupportRestoredLine = "Life support restored. Atmospheric regulation at nominal.";
        [TextArea] public string doorAnomalyLine = "Unauthorized door movement detected. No crew members are registered on this deck.";

        [Header("Post-signal lines")]
        [TextArea] public string signalLine = "Unknown signal source detected. I have no record of this transmission.";
        [TextArea] public string susLine = "You have been here before, Shiren.";

        public void Say(string text, float duration = 4f) =>
            EventBus.Publish(new SubtitleEvent("A.R.I.A.", text, duration));

        public void SaySystem(string text, float duration = 4f) =>
            EventBus.Publish(new SubtitleEvent("SYSTEM", text, duration));

        public void PlayOpeningSequence()
        {
            StartCoroutine(OpeningRoutine());
        }

        private IEnumerator OpeningRoutine()
        {
            yield return new WaitForSeconds(2.5f);
            Say(emergencyLine, 4f);
            yield return new WaitForSeconds(4.5f);
            Say(objectiveLine, 5f);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LifeSupportRestored>(OnLifeSupportRestored);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LifeSupportRestored>(OnLifeSupportRestored);
        }

        private void OnLifeSupportRestored(LifeSupportRestored evt)
        {
            StartCoroutine(RestoredRoutine());
        }

        private IEnumerator RestoredRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            Say(lifeSupportRestoredLine, 4.5f);
            yield return new WaitForSeconds(5f);
            Say(doorAnomalyLine, 5f);
        }
    }
}
