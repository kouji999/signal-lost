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
        [SerializeField] private SignalLost.UI.TitleScreen titleScreen;
        [SerializeField] private float deathRestartDelay = 4f;

        private void Start()
        {
            StoryFlagSystem.LoadFrom(System.Array.Empty<string>());
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<LifeSupportRestored>(OnLifeSupportRestored);
            EventBus.Subscribe<SliceCompleteEvent>(OnSliceComplete);
            EventBus.Subscribe<StoryFlagChanged>(OnFlagChanged);
            EventBus.Subscribe<DoorDenied>(OnDoorDenied);
            StartCoroutine(StartSequence());
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<LifeSupportRestored>(OnLifeSupportRestored);
            EventBus.Unsubscribe<SliceCompleteEvent>(OnSliceComplete);
            EventBus.Unsubscribe<StoryFlagChanged>(OnFlagChanged);
            EventBus.Unsubscribe<DoorDenied>(OnDoorDenied);
        }

        private readonly System.Collections.Generic.HashSet<string> _hinted = new();

        private void OnDoorDenied(DoorDenied evt)
        {
            if (_hinted.Contains(evt.DoorId)) return;
            _hinted.Add(evt.DoorId);

            switch (evt.DoorId)
            {
                case "door_comm":
                case "comm_array":
                    ObjectiveSystem.Instance.AddObjective("hint_keycard1", "Find the CREW KEYCARD",
                        "A.R.I.A. indicates the MEDBAY, through the west door of this deck.");
                    EventBus.Publish(new SubtitleEvent("A.R.I.A.",
                        "A crew keycard unlocks that door. The last one issued was logged in the MEDBAY. West. Please do not go alone.", 6f));
                    break;
                case "door_elevator":
                    ObjectiveSystem.Instance.AddObjective("hint_keycard2", "Find the SECURITY keycard (LVL-2)",
                        "The SECURITY OFFICE sits east of the COMMUNICATION DECK. Search it.");
                    EventBus.Publish(new SubtitleEvent("A.R.I.A.",
                        "Level-2 clearance only. The security office, east of the communication deck, holds such a card.", 6f));
                    break;
            }
        }

        private void OnFlagChanged(StoryFlagChanged evt)
        {
            if (evt.Key == "pickup:pickup_keycard" && evt.Value)
            {
                ObjectiveSystem.Instance.RemoveObjective("hint_keycard1");
                EventBus.Publish(new SubtitleEvent("SYSTEM", "CREW KEYCARD acquired — level 1. The COMMUNICATION DECK door will open now. [TAB] to view inventory", 5f));
            }
            else if (evt.Key == "pickup:pickup_power_cell" && evt.Value)
            {
                EventBus.Publish(new SubtitleEvent("SYSTEM", "POWER CELL acquired. The life-support console stands east of the HUB. [TAB] to view inventory", 5f));
            }
            else if (evt.Key == "pickup:pickup_keycard2" && evt.Value)
            {
                ObjectiveSystem.Instance.RemoveObjective("hint_keycard2");
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
            GameManager.Instance.SetState(GameState.Title);
            if (titleScreen != null)
                yield return new WaitUntil(() => titleScreen.Completed || Time.realtimeSinceStartup > 600f);

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
