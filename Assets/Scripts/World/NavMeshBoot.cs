using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace SignalLost.World
{
    // Bakes the navmesh at startup from code only (collectObjects.All scans the whole scene).
    // Nothing is serialized into the scene file, because serialized NavMeshSurface/baker
    // components corrupt level0 deserialization in Unity 6.6 player builds.
    public static class NavMeshBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (GameObject.Find("NavMeshBoot") != null) return;
            var go = new GameObject("NavMeshBoot");
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1 << LayerMask.NameToLayer("World");
            var t0 = Time.realtimeSinceStartup;
            surface.BuildNavMesh();
            Debug.Log($"[NavMeshBoot] baked in {(Time.realtimeSinceStartup - t0):F2}s");
        }
    }
}
