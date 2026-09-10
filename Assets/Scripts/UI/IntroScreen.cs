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

            foreach (var line in BootLines)
            {
                if (lineText != null) lineText.text = line;
                yield return new WaitForSeconds(1.1f);
            }

            if (lineText != null) lineText.text = string.Empty;

            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                root.alpha = 1f - (t / 1.4f);
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
