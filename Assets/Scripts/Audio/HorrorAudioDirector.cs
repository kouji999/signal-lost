using System.Collections;
using SignalLost.AI;
using SignalLost.Core;
using SignalLost.Interaction;
using SignalLost.Narrative;
using UnityEngine;

namespace SignalLost.Audio
{
    public class HorrorAudioDirector : MonoBehaviour
    {
        [SerializeField] private float masterVolume = 0.85f;

        private AudioSource _drone;
        private AudioSource _tension;
        private AudioSource _heartbeat;
        private AudioSource _whisper;
        private AudioSource[] _sfx;
        private int _sfxIdx;
        private float _targetTension, _targetHeart, _targetWhisper;
        private float _tensionLerpSpeed = 0.5f;
        private int _dangerLevel;
        private float _lastStinger = -10f;

        private void Awake()
        {
            _drone = AddLoop(SfxLibrary.Drone, 0.5f);
            _tension = AddLoop(SfxLibrary.Tension, 0f);
            _heartbeat = AddLoop(SfxLibrary.Heartbeat, 0f);
            _whisper = AddLoop(SfxLibrary.Whisper, 0f);

            _sfx = new AudioSource[8];
            for (int i = 0; i < _sfx.Length; i++)
            {
                _sfx[i] = gameObject.AddComponent<AudioSource>();
                _sfx[i].playOnAwake = false;
                _sfx[i].spatialBlend = 0f;
                _sfx[i].volume = masterVolume;
            }
        }

        private AudioSource AddLoop(AudioClip clip, float vol)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = vol;
            src.Play();
            return src;
        }

        public void Play2D(AudioClip clip, float vol = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            var src = _sfx[_sfxIdx % _sfx.Length];
            _sfxIdx++;
            src.pitch = pitch;
            src.PlayOneShot(clip, Mathf.Clamp01(vol * masterVolume));
        }

        private void OnEnable()
        {
            EventBus.Subscribe<InteractPerformed>(OnInteract);
            EventBus.Subscribe<DoorStateChanged>(OnDoor);
            EventBus.Subscribe<PlayerDamaged>(OnDamaged);
            EventBus.Subscribe<VitalsChanged>(OnVitals);
            EventBus.Subscribe<SubtitleEvent>(OnSubtitle);
            EventBus.Subscribe<FootstepPlayed>(OnStep);
            EventBus.Subscribe<EnemyChaseStarted>(OnChaseStart);
            EventBus.Subscribe<EnemyLostPlayer>(OnChaseEnd);
            EventBus.Subscribe<LifeSupportRestored>(OnLifeSupport);
            EventBus.Subscribe<StoryFlagChanged>(OnFlag);
            EventBus.Subscribe<ScannerResult>(OnScan);
            EventBus.Subscribe<PowerRestored>(OnPower);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<InteractPerformed>(OnInteract);
            EventBus.Unsubscribe<DoorStateChanged>(OnDoor);
            EventBus.Unsubscribe<PlayerDamaged>(OnDamaged);
            EventBus.Unsubscribe<VitalsChanged>(OnVitals);
            EventBus.Unsubscribe<SubtitleEvent>(OnSubtitle);
            EventBus.Unsubscribe<FootstepPlayed>(OnStep);
            EventBus.Unsubscribe<EnemyChaseStarted>(OnChaseStart);
            EventBus.Unsubscribe<EnemyLostPlayer>(OnChaseEnd);
            EventBus.Unsubscribe<LifeSupportRestored>(OnLifeSupport);
            EventBus.Unsubscribe<StoryFlagChanged>(OnFlag);
            EventBus.Unsubscribe<ScannerResult>(OnScan);
            EventBus.Unsubscribe<PowerRestored>(OnPower);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _tension.volume = Mathf.MoveTowards(_tension.volume, _targetTension * masterVolume, _tensionLerpSpeed * dt);
            _heartbeat.volume = Mathf.MoveTowards(_heartbeat.volume, _targetHeart * masterVolume, 1.5f * dt);
            _whisper.volume = Mathf.MoveTowards(_whisper.volume, _targetWhisper * masterVolume, 0.8f * dt);
        }

        private void OnInteract(InteractPerformed evt)
        {
            switch (evt.Target)
            {
                case PickupItem:
                    Play2D(SfxLibrary.Pickup);
                    break;
                case DoorController:
                    break;
                default:
                    Play2D(SfxLibrary.Beep);
                    break;
            }
        }

        private void OnDoor(DoorStateChanged evt)
        {
            Play2D(SfxLibrary.Door, 0.9f);
            StartCoroutine(ClankAfter(0.75f));
        }

        private IEnumerator ClankAfter(float s)
        {
            yield return new WaitForSecondsRealtime(s);
            Play2D(SfxLibrary.Clank, 0.8f, Random.Range(0.9f, 1.1f));
        }

        private void OnDamaged(PlayerDamaged evt)
        {
            Play2D(SfxLibrary.Hurt, 1f);
            TriggerStinger(2.5f);
        }

        private float _lowVitalsTimer;
        private void OnVitals(VitalsChanged evt)
        {
            bool critical = evt.Oxygen < 25f || evt.Health < 25f;
            if (critical && _dangerLevel == 0)
                _targetHeart = Mathf.Max(_targetHeart, 0.4f);
            else if (!critical && _dangerLevel == 0)
                _targetHeart = 0f;
        }

        private void OnSubtitle(SubtitleEvent evt)
        {
            if (evt.Speaker == "A.R.I.A." || evt.Speaker == "SYSTEM")
                Play2D(SfxLibrary.Static, 0.55f);
            else if (evt.Speaker == "UNKNOWN VOICE" || evt.Speaker == "RADIO VOICE")
            {
                Play2D(SfxLibrary.Static, 0.9f);
                _targetWhisper = Mathf.Max(_targetWhisper, 0.25f);
            }
            if (evt.Text != null && evt.Text.StartsWith("FABRICATED"))
                Play2D(SfxLibrary.Craft, 0.9f);
        }

        private float _lastScan = -1f;

        private void OnScan(ScannerResult evt)
        {
            if (evt.Lines == null) return;
            if (Time.time - _lastScan < 0.35f) return;
            _lastScan = Time.time;
            Play2D(SfxLibrary.Scan, 0.7f);
        }

        private void OnPower(PowerRestored evt)
        {
            Play2D(SfxLibrary.PowerUp, 1f);
        }

        private void OnStep(FootstepPlayed evt)
        {
            float vol = evt.Crouching ? 0.25f : evt.Sprinting ? 0.9f : 0.55f;
            Play2D(evt.Sprinting ? SfxLibrary.StepA : (Random.value > 0.5f ? SfxLibrary.StepA : SfxLibrary.StepB),
                vol, Random.Range(0.92f, 1.08f));
        }

        private void OnChaseStart(EnemyChaseStarted evt)
        {
            _dangerLevel = Mathf.Max(_dangerLevel, evt.Attacking ? 2 : 1);
            _targetTension = evt.Attacking ? 1f : 0.7f;
            _targetHeart = 0.85f;
            _targetWhisper = 0.35f;
            _tensionLerpSpeed = 2.2f;
            TriggerStinger(4f);
        }

        private void OnChaseEnd(EnemyLostPlayer evt)
        {
            _dangerLevel = Mathf.Max(0, _dangerLevel - 1);
            if (_dangerLevel == 0)
            {
                _targetTension = 0f;
                _targetHeart = 0f;
                _targetWhisper = 0f;
                _tensionLerpSpeed = 0.35f;
            }
        }

        private void OnLifeSupport(LifeSupportRestored evt)
        {
            _drone.pitch = 1.06f;
            _drone.volume = 0.35f * masterVolume;
        }

        private void OnFlag(StoryFlagChanged evt)
        {
            if (!evt.Value) return;
            if (evt.Key == StoryFlagKeys.EnteredResearch)
                TriggerStinger(6f);
        }

        private void TriggerStinger(float cooldown)
        {
            if (Time.time - _lastStinger < cooldown) return;
            _lastStinger = Time.time;
            Play2D(SfxLibrary.Stinger, 0.9f);
        }
    }
}
