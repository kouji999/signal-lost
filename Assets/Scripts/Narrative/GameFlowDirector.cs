using System.Collections;
using SignalLost.Core;
using SignalLost.Save;
using SignalLost.UI;
using UnityEngine;

namespace SignalLost.Narrative
{
    public class GameFlowDirector : MonoBehaviour
    {
        [SerializeField] private IntroScreen introScreen;
        [SerializeField] private float deathRestartDelay = 4f;

        private void Start()
        {
            StoryFlagSystem.LoadFrom(System.Array.Empty<string>());
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<LifeSupportRestored>(OnLifeSupportRestored);
            EventBus.Subscribe<SliceCompleteEvent>(OnSliceComplete);
            EventBus.Subscribe<StoryFlagChanged>(OnFlagChanged);
            StartCoroutine(StartSequence());
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<LifeSupportRestored>(OnLifeSupportRestored);
            EventBus.Unsubscribe<SliceCompleteEvent>(OnSliceComplete);
            EventBus.Unsubscribe<StoryFlagChanged>(OnFlagChanged);
        }

        private void OnFlagChanged(StoryFlagChanged evt)
        {
            if (evt.Key == "pickup:pickup_keycard2" && evt.Value)
            {
                StoryFlagSystem.Set(StoryFlagKeys.FoundKeycard2);
                EventBus.Publish(new SubtitleEvent("A.R.I.A.",
                    "A level-2 keycard. The elevator north of the array leads to the research decks. I would prefer you stayed.", 7f));
            }
            else if (evt.Key == StoryFlagKeys.EnteredResearch && evt.Value)
            {
                ObjectiveSystem.Instance.AddObjective("obj_core", "Reach The Station Core",
                    "The answer is at the bottom. The A.R.I.A. core lies beyond the laboratory.");
                EventBus.Publish(new SubtitleEvent("A.R.I.A.", "You were not supposed to find this. Nothing down here is yours to see.", 6f));
            }
            else if (evt.Key == StoryFlagKeys.EnteredCore && evt.Value)
            {
                ObjectiveSystem.Instance.AddObjective("obj_choice", "Decide",
                    "A.R.I.A. waits at the core. Three terminals. Three futures.");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5) && SaveManager.Instance != null) SaveManager.Instance.Save();
            if (Input.GetKeyDown(KeyCode.F9) && SaveManager.Instance != null) SaveManager.Instance.LoadAndApply();
        }

        private IEnumerator StartSequence()
        {
            GameManager.Instance.SetState(GameState.Intro);
            if (introScreen != null) yield return introScreen.Play();
            GameManager.Instance.SetState(GameState.Playing);

            var aria = FindAnyObjectByType<AriaController>();
            if (aria != null) aria.PlayOpeningSequence();

            ObjectiveSystem.Instance.AddObjective("obj_life_support", "Restore Life Support",
                "Find a POWER CELL in the STORAGE BAY (east), then restart the system at the console in this deck.");
        }

        private void OnLifeSupportRestored(LifeSupportRestored evt)
        {
            ObjectiveSystem.Instance.CompleteObjective("obj_life_support");
            ObjectiveSystem.Instance.AddObjective("obj_communication", "Restore Communication",
                "Find the CREW KEYCARD in the MEDBAY (west), then repair the array on the COMMUNICATION DECK (north).");
        }

        private void OnSliceComplete(SliceCompleteEvent evt)
        {
            if (introScreen != null) introScreen.ShowEnding("SIGNAL LOST", "END OF VERTICAL SLICE");
            GameManager.Instance.SetState(GameState.Ending);
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (introScreen != null) introScreen.ShowEnding("YOU DIED", evt.Reason);
            StartCoroutine(RestartRoutine());
        }

        private IEnumerator RestartRoutine()
        {
            GameManager.Instance.SetState(GameState.Ending);
            yield return new WaitForSecondsRealtime(deathRestartDelay);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
        }
    }
}
