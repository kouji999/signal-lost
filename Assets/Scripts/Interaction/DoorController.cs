using System.Collections;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Interaction
{
    public class DoorController : MonoBehaviour, IInteractable
    {
        [SerializeField] private string doorId = "door_01";
        [SerializeField] private Transform slidePanel;
        [SerializeField] private float openOffset = 2.4f;
        [SerializeField] private float openSpeed = 2.2f;
        [SerializeField] private bool startOpen;
        [SerializeField] private int accessLevel;
        [SerializeField] private string lockedHint = "ACCESS DENIED - SECURITY LEVEL REQUIRED";
        [SerializeField] private bool lockedByFlag;
        [SerializeField] private string requiredFlag;

        private Vector3 _closedPos;
        private Vector3 _openPos;
        private bool _open;
        private Coroutine _anim;

        public string DoorId => doorId;
        public bool IsOpen => _open;
        public string Prompt => _open ? "[E] CLOSE DOOR" : "[E] OPEN DOOR";

        private void Awake()
        {
            _closedPos = slidePanel.localPosition;
            _openPos = _closedPos + new Vector3(0f, openOffset, 0f);
            _open = startOpen;
            slidePanel.localPosition = _open ? _openPos : _closedPos;
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
                    return false;
                }
            }
            if (lockedByFlag && !string.IsNullOrEmpty(requiredFlag) && !StoryFlagSystem.IsSet(requiredFlag))
            {
                EventBus.Publish(new SubtitleEvent("SYSTEM", lockedHint, 2.5f));
                return false;
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
            var target = open ? _openPos : _closedPos;
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateTo(target));
            if (broadcast) EventBus.Publish(new DoorStateChanged(doorId, open));
        }

        private IEnumerator AnimateTo(Vector3 target)
        {
            while (Vector3.Distance(slidePanel.localPosition, target) > 0.01f)
            {
                slidePanel.localPosition = Vector3.MoveTowards(slidePanel.localPosition, target, openSpeed * Time.deltaTime);
                yield return null;
            }
            slidePanel.localPosition = target;
        }
    }

    public static class DoorRegistry
    {
        private static readonly System.Collections.Generic.List<DoorController> Doors = new();
        public static System.Collections.Generic.IReadOnlyList<DoorController> All => Doors;
        public static void Register(DoorController d) { if (!Doors.Contains(d)) Doors.Add(d); }
        public static void Unregister(DoorController d) => Doors.Remove(d);
        public static DoorController Find(string id) => Doors.Find(d => d.DoorId == id);
    }
}
