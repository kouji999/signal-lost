using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Player
{
    public class PlayerVitals : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxOxygen = 100f;
        [SerializeField] private float maxBattery = 100f;
        [SerializeField] private float oxygenDrainPerSecond = 0.07f;
        [SerializeField] private float suffocationDamagePerSecond = 6f;

        public float Health { get; private set; }
        public float Oxygen { get; private set; }
        public float Battery { get; private set; }

        public float MaxHealth => maxHealth;
        public float MaxOxygen => maxOxygen;
        public float MaxBattery => maxBattery;

        public bool IsDead => Health <= 0f;

        private void Awake()
        {
            Health = maxHealth;
            Oxygen = maxOxygen;
            Battery = maxBattery;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (IsDead) return;

            if (!StoryFlagSystem.IsSet(SignalLost.Narrative.StoryFlagKeys.LifeSupportOnline))
            {
                Oxygen = Mathf.Max(0f, Oxygen - oxygenDrainPerSecond * Time.deltaTime);
            }

            if (Oxygen <= 0f)
            {
                Damage(suffocationDamagePerSecond * Time.deltaTime, silent: true);
            }

            Publish();
        }

        public void Damage(float amount, bool silent = false)
        {
            if (IsDead || amount <= 0f) return;
            Health = Mathf.Max(0f, Health - amount);
            if (!silent) EventBus.Publish(new PlayerDamaged(amount));
            if (Health <= 0f) EventBus.Publish(new GameOverEvent("VITALS FLATLINE"));
            Publish();
        }

        public void Heal(float amount)
        {
            Health = Mathf.Min(maxHealth, Health + amount);
            Publish();
        }

        public void RefillOxygen(float amount)
        {
            Oxygen = Mathf.Min(maxOxygen, Oxygen + amount);
            Publish();
        }

        public void ChargeBattery(float amount)
        {
            Battery = Mathf.Min(maxBattery, Battery + amount);
            Publish();
        }

        public bool ConsumeBattery(float amount)
        {
            if (Battery < amount) return false;
            Battery -= amount;
            Publish();
            return true;
        }

        public void Restore(float health, float oxygen, float battery)
        {
            Health = Mathf.Clamp(health, 0f, maxHealth);
            Oxygen = Mathf.Clamp(oxygen, 0f, maxOxygen);
            Battery = Mathf.Clamp(battery, 0f, maxBattery);
            Publish();
        }

        private void Publish() => EventBus.Publish(new VitalsChanged(Health, Oxygen, Battery));
    }
}
