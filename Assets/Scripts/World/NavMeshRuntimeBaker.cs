using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace SignalLost.World
{
    // NavMesh is baked once at scene start (player and editor) instead of being
    // serialized into the scene file — embedded NavMeshData blobs corrupt level0
    // in Unity 6.6 player builds.
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavMeshRuntimeBaker : MonoBehaviour
    {
        private void Awake()
        {
            var surface = GetComponent<NavMeshSurface>();
            if (surface == null) return;
            if (surface.navMeshData != null) return;
            var t0 = Time.realtimeSinceStartup;
            surface.BuildNavMesh();
            Debug.Log($"[NavMeshRuntimeBaker] baked in {(Time.realtimeSinceStartup - t0):F2}s");
        }
    }
}
