using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Player
{
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private float sensitivity = 1.5f;
        [SerializeField] private float maxPitch = 85f;

        private float _pitch;
        private float _yaw;

        private void Start()
        {
            sensitivity = SignalLost.UI.GameSettings.Sensitivity;
            var e = transform.eulerAngles;
            _yaw = e.y;
            _pitch = cameraHolder != null ? cameraHolder.localEulerAngles.x : 0f;
            if (_pitch > 180f) _pitch -= 360f;
        }

        private void LateUpdate()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            _yaw += Input.GetAxis("Mouse X") * sensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            _pitch = Mathf.Clamp(_pitch, -maxPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cameraHolder != null)
                cameraHolder.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
