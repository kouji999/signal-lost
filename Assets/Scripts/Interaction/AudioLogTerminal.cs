using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Interaction
{
    public class AudioLogTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private Narrative.AudioLogDefinition log;
        [SerializeField] private bool onlyOnce = true;
        private bool _played;

        public string Prompt => $"[E] PLAY LOG — {log.title}";

        public bool CanInteract(GameObject interactor) => !(onlyOnce && _played);

        public void Interact(GameObject interactor)
        {
            if (onlyOnce && _played) return;
            _played = true;
            if (Narrative.LogLibrary.Instance != null) Narrative.LogLibrary.Instance.Discover(log.logId);
            StoryFlagSystem.Set(StoryFlagKeys.FoundFirstLog);
            EventBus.Publish(new SubtitleEvent("LOG " + log.logId, log.content, Mathf.Max(6f, log.content.Length * 0.055f)));
        }
    }
}
