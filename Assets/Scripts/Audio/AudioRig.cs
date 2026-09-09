using System.Collections;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Audio
{
    public class AudioRig : MonoBehaviour
    {
        [SerializeField] private float masterVolume = 0.7f;

        private AudioSource _hum;
        private AudioSource _vent;
        private AudioSource _creak;

        private void Awake()
        {
            _hum = gameObject.AddComponent<AudioSource>();
            _vent = gameObject.AddComponent<AudioSource>();
            _creak = gameObject.AddComponent<AudioSource>();

            foreach (var src in new[] { _hum, _vent, _creak })
            {
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.volume = masterVolume;
            }

            _hum.clip = GenerateHum();
            _hum.loop = true;
            _hum.volume = masterVolume * 0.5f;
            _hum.Play();

            _vent.clip = GenerateVent();
            _vent.loop = true;
            _vent.volume = 0f;

            _creak.clip = GenerateCreak();
            _creak.volume = masterVolume * 0.8f;

            StartCoroutine(CreakRoutine());
        }

        private void OnEnable() => EventBus.Subscribe<LifeSupportRestored>(OnLifeSupport);
        private void OnDisable() => EventBus.Unsubscribe<LifeSupportRestored>(OnLifeSupport);

        private void OnLifeSupportRestored(LifeSupportRestored evt) => OnLifeSupport(evt);

        private void OnLifeSupport(LifeSupportRestored evt)
        {
            StartCoroutine(RaiseVent());
        }

        private IEnumerator RaiseVent()
        {
            float t = 0f;
            while (t < 3f)
            {
                t += Time.deltaTime;
                _vent.volume = Mathf.Lerp(0f, masterVolume * 0.45f, t / 3f);
                yield return null;
            }
        }

        private IEnumerator CreakRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(18f, 55f));
                if (!_creak.isPlaying)
                {
                    _creak.pitch = Random.Range(0.8f, 1.25f);
                    _creak.Play();
                }
            }
        }

        private static AudioClip GenerateHum()
        {
            const int sr = 44100;
            const int len = sr * 4;
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                float mod = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.23f * t);
                data[i] = mod * (0.32f * Mathf.Sin(2f * Mathf.PI * 52f * t)
                               + 0.14f * Mathf.Sin(2f * Mathf.PI * 104f * t)
                               + 0.06f * Mathf.Sin(2f * Mathf.PI * 156f * t));
            }
            LoopSmooth(data, sr);
            var clip = AudioClip.Create("hum", len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateVent()
        {
            const int sr = 44100;
            const int len = sr * 3;
            var data = new float[len];
            float last = 0f;
            for (int i = 0; i < len; i++)
            {
                float white = Random.value * 2f - 1f;
                last = last * 0.96f + white * 0.04f;
                data[i] = last * 2.2f;
            }
            LoopSmooth(data, sr);
            var clip = AudioClip.Create("vent", len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateCreak()
        {
            const int sr = 44100;
            const int len = sr * 2;
            var data = new float[len];
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float freq = Mathf.Lerp(140f, 60f, t);
                phase += 2f * Mathf.PI * freq / sr;
                float env = Mathf.Sin(Mathf.PI * t);
                data[i] = env * (0.6f * Mathf.Sin(phase) + 0.15f * Mathf.Sin(phase * 2.7f) + 0.08f * (Random.value * 2f - 1f));
            }
            var clip = AudioClip.Create("creak", len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void LoopSmooth(float[] data, int sr)
        {
            int crossfade = sr / 20;
            for (int i = 0; i < crossfade; i++)
            {
                float w = (float)i / crossfade;
                data[i] = data[i] * w + data[data.Length - crossfade + i] * (1f - w);
            }
        }
    }
}
