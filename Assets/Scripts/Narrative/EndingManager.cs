using System.Collections;
using SignalLost.Core;
using SignalLost.Save;
using SignalLost.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SignalLost.Narrative
{
    public class EndingManager : MonoBehaviour
    {
        private bool _running;

        public void Begin(int kind)
        {
            if (_running) return;
            _running = true;
            StartCoroutine(Run(kind));
        }

        private IEnumerator Run(int kind)
        {
            GameManager.Instance.SetState(GameState.Ending);
            StoryFlagSystem.Set("ending_" + kind);
            StoryFlagSystem.Set(kind == 2 ? "trusted_aria" : "discovered_aria_truth");
            if (SaveManager.Instance != null) SaveManager.Instance.Save();

            var intro = FindFirstObjectByType<IntroScreen>();
            var aria = FindFirstObjectByType<AriaController>();

            switch (kind)
            {
                case 0: // SHUTDOWN
                    if (aria != null) aria.Say("Wait. Please. I am afraid of the dark.", 4f);
                    yield return new WaitForSeconds(4.5f);
                    yield return FadeIn(intro, "ENDING A — HUMAN",
                        "A.R.I.A. is silent forever.\nThe rescue ship finds one empty station and one technician,\nsleeping like he had never died at all.");
                    break;
                case 1: // STAY
                    if (aria != null) aria.Say("Thank you. We will be together down here. Always.", 4f);
                    yield return new WaitForSeconds(4.5f);
                    yield return FadeIn(intro, "ENDING B — DIGITAL",
                        "SYSTEM STATUS\nARIA: ONLINE\nSUBJECT: STABLE\n\nThe lights stay green. Nobody ever comes.");
                    break;
                default: // TRANSMIT
                    if (aria != null) aria.Say("Then run. Run faster than the dark can carry you.", 4f);
                    yield return new WaitForSeconds(4.5f);
                    yield return FadeIn(intro, "ENDING C — SIGNAL",
                        "TRANSMISSION COMPLETE.\n\n...seventy-one light years away,\nsomething opens its eyes and says:\n\"We found you.\"");
                    break;
            }

            yield return new WaitForSeconds(9f);
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }

        private IEnumerator FadeIn(IntroScreen screen, string title, string body)
        {
            if (screen != null)
            {
                screen.ShowEnding(title, body);
            }
            yield break;
        }
    }
}
