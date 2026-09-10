using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Transform _camera;
        [SerializeField] private float _maxDistance = 2.5f;
        [SerializeField] private LayerMask _interactableMask = ~0;

        private Interaction.IInteractable _focused;

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.State != GameState.Playing) { ClearFocus(); return; }
            FindTarget();

            if (_focused != null && Input.GetKeyDown(KeyCode.E))
            {
                _focused.Interact(gameObject);
                EventBus.Publish(new InteractPerformed(_focused, gameObject));
            }

            var inv = GetComponent<Inventory.Inventory>();
            if (inv != null)
            {
                for (int i = 0; i < 6; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i)) inv.UseSlot(i);
            }
        }

        private void FindTarget()
        {
            var newTarget = default(Interaction.IInteractable);
            if (_camera != null)
            {
                var ray = new Ray(_camera.position, _camera.forward);
                if (Physics.Raycast(ray, out var hit, _maxDistance, _interactableMask, QueryTriggerInteraction.Collide))
                {
                    newTarget = FindInteractable(hit.collider.transform);
                }
            }

            if (!ReferenceEquals(newTarget, _focused))
            {
                _focused = newTarget;
                EventBus.Publish(new InteractableFocused(_focused));
            }
        }

        private static Interaction.IInteractable FindInteractable(Transform t)
        {
            int safety = 0;
            while (t != null && safety++ < 16)
            {
                if (t.TryGetComponent(out Interaction.IInteractable found)) return found;
                t = t.parent;
            }
            return null;
        }

        private void ClearFocus()
        {
            if (_focused == null) return;
            _focused = null;
            EventBus.Publish(new InteractableFocused(null));
        }
    }
}
