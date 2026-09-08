using SignalLost.AI;
using UnityEngine;

namespace SignalLost.Player
{
    public class NoiseSource : MonoBehaviour
    {
        [SerializeField] private float walkRadius = 7f;
        [SerializeField] private float sprintRadius = 14f;
        [SerializeField] private float crouchRadius = 1.8f;
        [SerializeField] private float interval = 0.42f;

        private float _timer;

        public void EmitFootstep(bool crouching, bool sprinting)
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = crouching ? interval * 1.6f : interval;
            var radius = crouching ? crouchRadius : sprinting ? sprintRadius : walkRadius;
            NoiseSystem.EmitNoise(transform.position, radius, gameObject);
        }

        public static void EmitImpact(Vector3 position, float radius, GameObject source)
        {
            NoiseSystem.EmitNoise(position, radius, source);
        }
    }
}
