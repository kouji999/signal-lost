using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Player
{
    public class Flashlight : MonoBehaviour
    {
        [SerializeField] private Light spotLight;
        [SerializeField] private PlayerVitals vitals;
        [SerializeField] private float drainPerSecond = 0.12f;
        [SerializeField] private float flickerThreshold = 15f;

        public bool IsOn { get; private set; } = true;

        private float _flickerTimer;

        private void Start()
        {
            SetOn(true);
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (Input.GetKeyDown(KeyCode.F)) SetOn(!IsOn);
            if (!IsOn) return;

            if (!vitals.ConsumeBattery(drainPerSecond * Time.deltaTime))
            {
                SetOn(false);
                return;
            }

            if (vitals.Battery < flickerThreshold)
            {
                _flickerTimer -= Time.deltaTime;
                if (_flickerTimer <= 0f)
                {
                    spotLight.enabled = Random.value > 0.25f;
                    _flickerTimer = Random.Range(0.03f, 0.18f);
                }
            }
            else
            {
                spotLight.enabled = true;
            }
        }

        public void SetOn(bool on)
        {
            if (on && vitals.Battery <= 0f) on = false;
            IsOn = on;
            if (spotLight != null) spotLight.enabled = on;
            EventBus.Publish(new FlashlightToggled(on));
        }
    }
}
