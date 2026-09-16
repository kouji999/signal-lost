using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SignalLost.UI
{
    public class IntroScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Text lineText;
        [SerializeField] private CanvasGroup deathPanel;
        [SerializeField] private Text deathText;

        private static readonly string[] BootLines =
        {
            "WARNING",
            "LIFE SUPPORT: 12%",
            "COMMUNICATION: OFFLINE",
            "REACTOR: CRITICAL",
            "CREW STATUS: UNKNOWN",
            "EMERGENCY RECOVERY LOG // KEPLER-9 DEEP RESEARCH STATION",
            "SURVIVOR: TECHNICIAN S. HALE - POD 04",
            "UNCONSCIOUS: 71 HOURS"
        };

        public IEnumerator Play()
        {
            if (root == null) yield break;

            root.alpha = 1f;
            bool skipped = false;

            foreach (var line in BootLines)
            {
                if (lineText != null) lineText.text = line;
                float t = 0f;
                while (t < 1.1f)
                {
                    t += Time.unscaledDeltaTime;
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    {
                        skipped = true;
                        break;
                    }
                    yield return null;
                }
                if (skipped) break;
            }

            if (lineText != null) lineText.text = string.Empty;

            float f = 0f;
            float fadeTime = skipped ? 0.6f : 1.4f;
            while (f < fadeTime)
            {
                f += Time.unscaledDeltaTime;
                root.alpha = 1f - (f / fadeTime);
                yield return null;
            }
            root.alpha = 0f;
        }

        public void ShowEnding(string title, string subtitle)
        {
            if (deathPanel == null) return;
            deathPanel.alpha = 1f;
            if (deathText != null) deathText.text = title + "\n" + subtitle;
        }
    }
}
