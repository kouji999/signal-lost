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
            yield return new WaitForSeconds(1.8f);
            Say("Emergency protocol activated.", 3.5f);
            yield return new WaitForSeconds(4f);
            Say("I am A.R.I.A., operations intelligence of Kepler-9. You are safe. For now.", 5f);
            yield return new WaitForSeconds(5.5f);
            Say("Your first task: restore Life Support. Find a power cell in the STORAGE BAY, east of this deck.", 6f);
            yield return new WaitForSeconds(6.5f);
            SaySystem("CONTROLS — [E] INTERACT   [F] FLASHLIGHT   [CTRL] CROUCH   [SHIFT] SPRINT   [ESC] PAUSE", 8f);
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
            Say("Life support restored. Atmospheric regulation at nominal.", 4.5f);
            yield return new WaitForSeconds(5f);
            Say("Next: re-establish communication with Earth. The COMMUNICATION DECK is north. You will need a crew keycard.", 6f);
            yield return new WaitForSeconds(6.5f);
            Say("I detected... unauthorized door movement. No crew members are registered on this deck.", 5.5f);
        }
    }
}
