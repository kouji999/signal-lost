using UnityEngine;

namespace SignalLost.World
{
    public class FlickerLight : MonoBehaviour
    {
        [SerializeField] private float minInterval = 0.05f;
        [SerializeField] private float maxInterval = 0.4f;
        [SerializeField] private float offChance = 0.35f;
        [SerializeField] private bool startFlickering = true;

        private Light _light;
        private float _timer;
        private float _baseIntensity;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light != null ? _light.intensity : 1f;
            enabled = startFlickering;
        }

        private void Update()
        {
            if (_light == null) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = Random.Range(minInterval, maxInterval);
            _light.intensity = Random.value < offChance ? 0f : _baseIntensity * Random.Range(0.6f, 1f);
        }

        public void SetFlickering(bool on)
        {
            enabled = on;
            if (!on && _light != null) _light.intensity = _baseIntensity;
        }
    }
}
