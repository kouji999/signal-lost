using System.Collections;
using SignalLost.Core;
using UnityEngine;
using UnityEngine.AI;

namespace SignalLost.Interaction
{
    public class DoorController : MonoBehaviour, IInteractable
    {
        [SerializeField] private string doorId = "door_01";
        [SerializeField] private Transform slidePanel;
        [SerializeField] private float openSpeed = 2.2f;
        [SerializeField] private bool startOpen;
        [SerializeField] private int accessLevel;
        [SerializeField] private string lockedHint = "ACCESS DENIED - SECURITY LEVEL REQUIRED";

        private float _panelHeight;
        private float _topY;
        private bool _open;
        private Coroutine _anim;
        private UnityEngine.AI.NavMeshObstacle _obstacle;

        public string DoorId => doorId;
        public bool IsOpen => _open;
        public string Prompt => _open ? "[E] CLOSE DOOR"
            : accessLevel > 0 ? $"[E] LOCKED - REQUIRES LVL-{accessLevel} KEYCARD"
            : "[E] OPEN DOOR";

        private void Awake()
        {
            _panelHeight = slidePanel.localScale.y;
            _topY = slidePanel.localPosition.y + _panelHeight / 2f;
            _obstacle = GetComponent<NavMeshObstacle>();
            if (_obstacle == null)
            {
                _obstacle = gameObject.AddComponent<NavMeshObstacle>();
                _obstacle.shape = NavMeshObstacleShape.Box;
                _obstacle.carving = true;
                _obstacle.center = new Vector3(0f, 1.5f, 0f);
                _obstacle.size = new Vector3(3f, 3f, 0.5f);
            }
            if (startOpen) SetOpen(true);
            else ApplyProgress(0f);
            DoorRegistry.Register(this);
        }

        private void OnDestroy() => DoorRegistry.Unregister(this);

        public bool CanInteract(GameObject interactor)
        {
            if (accessLevel > 0)
            {
                var inv = interactor.GetComponent<Inventory.Inventory>();
                if (inv == null || inv.HighestAccessLevel() < accessLevel)
                {
                    EventBus.Publish(new SubtitleEvent("SYSTEM", lockedHint, 2.5f));
                    EventBus.Publish(new DoorDenied(doorId, accessLevel));
                    return false;
                }
            }
            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            SetOpen(!_open, broadcast: true);
        }

        public void SetOpen(bool open, bool broadcast = false)
        {
            _open = open;
            if (_anim != null) StopCoroutine(_anim);
            _anim = open ? StartCoroutine(ShutterDown()) : StartCoroutine(ShutterUp());
            if (broadcast) EventBus.Publish(new DoorStateChanged(doorId, open));
        }

        private IEnumerator ShutterDown()
        {
            float p = 0f;
            while (p < 1f)
            {
                p = Mathf.MoveTowards(p, 1f, openSpeed * Time.deltaTime);
                ApplyProgress(p);
                yield return null;
            }
            ApplyProgress(1f);
        }

        private IEnumerator ShutterUp()
        {
            float p = 1f;
            ApplyProgress(0f);
            while (p > 0f)
            {
                p = Mathf.MoveTowards(p, 0f, openSpeed * Time.deltaTime);
                ApplyProgress(p);
                yield return null;
            }
        }

        private void ApplyProgress(float p)
        {
            float s = Mathf.Lerp(1f, 0.02f, p);
            var sc = slidePanel.localScale;
            sc.y = _panelHeight * s;
            slidePanel.localScale = sc;
            var pos = slidePanel.localPosition;
            pos.y = _topY - (_panelHeight * s) / 2f;
            slidePanel.localPosition = pos;
            if (_obstacle != null) _obstacle.enabled = s > 0.2f;
        }
    }

    public static class DoorRegistry
    {
        private static readonly System.Collections.Generic.List<DoorController> Doors = new();
        public static System.Collections.Generic.IReadOnlyList<DoorController> All => Doors;
        public static void Register(DoorController d) { if (!Doors.Contains(d)) Doors.Add(d); }
        public static void Unregister(DoorController d) => Doors.Remove(d);
        public static DoorController Find(string id)
        {
            for (int i = Doors.Count - 1; i >= 0; i--)
            {
                if (Doors[i] == null) { Doors.RemoveAt(i); continue; }
                if (Doors[i].DoorId == id) return Doors[i];
            }
            return null;
        }
    }
}
