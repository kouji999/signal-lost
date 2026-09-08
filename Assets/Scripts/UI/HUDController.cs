using System.Collections;
using SignalLost.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SignalLost.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Vitals")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Image oxygenFill;
        [SerializeField] private Image batteryFill;

        [Header("Interaction prompt")]
        [SerializeField] private Text promptText;

        [Header("Objective")]
        [SerializeField] private Text objectiveText;

        [Header("Subtitles")]
        [SerializeField] private Text speakerText;
        [SerializeField] private Text subtitleText;

        private Player.PlayerVitals _vitals;
        private Coroutine _subtitleRoutine;

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) _vitals = player.GetComponent<Player.PlayerVitals>();

            EventBus.Subscribe<VitalsChanged>(OnVitalsChanged);
            EventBus.Subscribe<InteractableFocused>(OnFocused);
            EventBus.Subscribe<ObjectiveChanged>(OnObjectiveChanged);
            EventBus.Subscribe<SubtitleEvent>(OnSubtitle);

            RefreshObjectives();
            SetPrompt(null);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<VitalsChanged>(OnVitalsChanged);
            EventBus.Unsubscribe<InteractableFocused>(OnFocused);
            EventBus.Unsubscribe<ObjectiveChanged>(OnObjectiveChanged);
            EventBus.Unsubscribe<SubtitleEvent>(OnSubtitle);
        }

        private void OnVitalsChanged(VitalsChanged evt)
        {
            if (_vitals == null) return;
            if (healthFill != null) healthFill.fillAmount = _vitals.Health / _vitals.MaxHealth;
            if (oxygenFill != null) oxygenFill.fillAmount = _vitals.Oxygen / _vitals.MaxOxygen;
            if (batteryFill != null) batteryFill.fillAmount = _vitals.Battery / _vitals.MaxBattery;
        }

        private void OnFocused(InteractableFocused evt)
        {
            SetPrompt(evt.Target?.Prompt);
        }

        private void SetPrompt(string text)
        {
            if (promptText == null) return;
            promptText.text = text ?? string.Empty;
            promptText.enabled = !string.IsNullOrEmpty(text);
        }

        private void OnObjectiveChanged(ObjectiveChanged evt) => RefreshObjectives();

        private void RefreshObjectives()
        {
            if (objectiveText == null || ObjectiveSystem.Instance == null) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("OBJECTIVE");
            foreach (var obj in ObjectiveSystem.Instance.ActiveObjectives)
            {
                sb.AppendLine(obj.Completed ? $"<s>{obj.Title}</s>" : obj.Title);
            }
            objectiveText.text = sb.ToString();
        }

        private void OnSubtitle(SubtitleEvent evt)
        {
            if (subtitleText == null) return;
            if (_subtitleRoutine != null) StopCoroutine(_subtitleRoutine);
            _subtitleRoutine = StartCoroutine(ShowSubtitle(evt));
        }

        private IEnumerator ShowSubtitle(SubtitleEvent evt)
        {
            if (speakerText != null)
            {
                speakerText.text = evt.Speaker;
                speakerText.enabled = !string.IsNullOrEmpty(evt.Speaker);
            }
            subtitleText.text = evt.Text;
            yield return new WaitForSeconds(evt.Duration);
            subtitleText.text = string.Empty;
            if (speakerText != null) speakerText.enabled = false;
        }
    }
}
