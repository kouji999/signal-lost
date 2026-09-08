using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 3.0f;
        [SerializeField] private float sprintSpeed = 5.2f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [SerializeField] private float gravity = -19.6f;
        [SerializeField] private NoiseSource noiseSource;

        private CharacterController _cc;
        private Vector3 _velocity;
        private float _groundedTimer;

        public bool Crouching { get; private set; }
        public bool Sprinting { get; private set; }
        public Vector3 PlanarVelocity { get; private set; }
        public bool IsMoving => PlanarVelocity.sqrMagnitude > 0.01f;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                PlanarVelocity = Vector3.zero;
                ApplyGravity();
                return;
            }

            Crouching = Input.GetKey(KeyCode.LeftControl);
            var move = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
            move = Vector3.ClampMagnitude(move, 1f);

            Sprinting = Input.GetKey(KeyCode.LeftShift) && !Crouching && move.magnitude > 0.1f;
            var speed = Crouching ? crouchSpeed : Sprinting ? sprintSpeed : walkSpeed;

            var desired = transform.forward * move.z * speed + transform.right * move.x * speed;
            PlanarVelocity = Vector3.MoveTowards(PlanarVelocity, desired, 20f * Time.deltaTime);

            if (_cc.isGrounded)
            {
                _groundedTimer = 0.12f;
                if (_velocity.y < 0f) _velocity.y = -2f;
            }
            else
            {
                _groundedTimer -= Time.deltaTime;
            }

            ApplyGravity();
            _cc.Move((PlanarVelocity + _velocity) * Time.deltaTime);

            if (noiseSource != null && move.magnitude > 0.1f)
                noiseSource.EmitFootstep(Crouching, Sprinting);
        }

        private void ApplyGravity()
        {
            if (_groundedTimer > 0f && _velocity.y <= 0f) _velocity.y = -2f;
            else _velocity.y += gravity * Time.deltaTime;
        }
    }
}
