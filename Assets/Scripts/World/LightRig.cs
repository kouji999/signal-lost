using SignalLost.Core;
using UnityEngine;

namespace SignalLost.World
{
    public class LightRig : MonoBehaviour
    {
        [SerializeField] private Light[] mainLights;
        [SerializeField] private float targetIntensity = 1.6f;
        [SerializeField] private float fadeDuration = 3f;

        private bool _triggered;

        private void OnEnable() => EventBus.Subscribe<LifeSupportRestored>(OnRestored);
        private void OnDisable() => EventBus.Unsubscribe<LifeSupportRestored>(OnRestored);

        private void OnRestored(LifeSupportRestored evt)
        {
            if (_triggered) return;
            _triggered = true;
            StartCoroutine(FadeIn());
        }

        private System.Collections.IEnumerator FadeIn()
        {
            float t = 0f;
            foreach (var l in mainLights)
                if (l != null) l.enabled = true;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, targetIntensity, t / fadeDuration);
                foreach (var l in mainLights)
                    if (l != null) l.intensity = k;
                yield return null;
            }
        }
    }
}
