using System;
using System.Collections.Generic;
using UnityEngine;

namespace SignalLost.AI
{
    public class EnemyPerception : MonoBehaviour
    {
        [SerializeField] private Transform eye;
        [SerializeField] private float visionRange = 14f;
        [SerializeField] private float visionAngle = 70f;
        [SerializeField] private float hearingRadiusMultiplier = 1.0f;
        [SerializeField] private LayerMask occlusionMask;

        public Vector3? LastKnownPlayerPosition { get; private set; }
        public event Action<Vector3> Heard;

        public bool CanSee(Transform target)
        {
            if (target == null) return false;
            var eyePos = eye != null ? eye.position : transform.position;
            var targetPos = target.position + Vector3.up * 1.2f;
            var toTarget = targetPos - eyePos;
            if (toTarget.magnitude > visionRange) return false;
            if (Vector3.Angle(transform.forward, toTarget) > visionAngle * 0.5f) return false;
            if (Physics.Linecast(eyePos, targetPos, occlusionMask)) return false;
            return true;
        }

        public void OnNoiseHeard(Vector3 position, float radius, GameObject source)
        {
            var dist = Vector3.Distance(transform.position, position);
            if (dist > radius * hearingRadiusMultiplier) return;
            LastKnownPlayerPosition = position;
            Heard?.Invoke(position);
        }

        public void ReportPlayerSighting(Vector3 playerPos) => LastKnownPlayerPosition = playerPos;
    }
}
