using SignalLost.Core;
using UnityEngine;

namespace SignalLost.World
{
    public class LightRig : MonoBehaviour
    {
        [SerializeField] private Light[] mainLights;
        [SerializeField] private Light[] powerLights;
        [SerializeField] private float targetIntensity = 1.6f;
        [SerializeField] private float fadeDuration = 3f;

        private bool _triggered;
        private bool _powerTriggered;

        private void OnEnable()
        {
            EventBus.Subscribe<LifeSupportRestored>(OnRestored);
            EventBus.Subscribe<PowerRestored>(OnPower);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LifeSupportRestored>(OnRestored);
            EventBus.Unsubscribe<PowerRestored>(OnPower);
        }

        private void OnRestored(LifeSupportRestored evt)
        {
            if (_triggered) return;
            _triggered = true;
            StartCoroutine(FadeIn(mainLights));
        }

        private void OnPower(PowerRestored evt)
        {
            if (_powerTriggered || powerLights == null || powerLights.Length == 0) return;
            _powerTriggered = true;
            StartCoroutine(FadeIn(powerLights));
        }

        private System.Collections.IEnumerator FadeIn(Light[] group)
        {
            float t = 0f;
            foreach (var l in group)
                if (l != null) l.enabled = true;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, targetIntensity, t / fadeDuration);
                foreach (var l in group)
                    if (l != null) l.intensity = k;
                yield return null;
            }
        }
    }
}
