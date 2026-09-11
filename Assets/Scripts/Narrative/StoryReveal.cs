using System;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Narrative
{
    [Serializable]
    public class RevealLine
    {
        public string speaker;
        [TextArea] public string text;
        public float delay;
    }

    public class StoryReveal : MonoBehaviour
    {
        [SerializeField] private RevealLine[] lines;
        [SerializeField] private string setFlag;
        [SerializeField] private bool repeatable;

        private bool _done;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (_done && !repeatable) return;
            _done = true;
            if (!string.IsNullOrEmpty(setFlag)) StoryFlagSystem.Set(setFlag);
            StartCoroutine(Play());
        }

        private System.Collections.IEnumerator Play()
        {
            foreach (var l in lines)
            {
                if (l.delay > 0f) yield return new WaitForSeconds(l.delay);
                if (string.IsNullOrEmpty(l.text)) continue;
                var speaker = string.IsNullOrEmpty(l.speaker) ? "SYSTEM" : l.speaker;
                var dur = Mathf.Max(3f, l.text.Length * 0.055f);
                EventBus.Publish(new SubtitleEvent(speaker, l.text, dur));
            }
        }
    }
}
