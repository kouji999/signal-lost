using System.Text;
using SignalLost.AI;
using SignalLost.Core;
using SignalLost.Interaction;
using UnityEngine;

namespace SignalLost.Player
{
    public class Scanner : MonoBehaviour
    {
        [SerializeField] private float scanRange = 18f;
        [SerializeField] private float scanInterval = 0.4f;
        [SerializeField] private float batteryDrainPerSecond = 0.2f;
        [SerializeField] private int scanMask = (1 << 10) | (1 << 11);

        private PlayerVitals _vitals;
        private Transform _cam;
        private float _timer;
        private readonly Collider[] _hits = new Collider[24];

        public bool Active { get; private set; }

        private void Awake()
        {
            _vitals = GetComponent<PlayerVitals>();
            var cam = GameObject.FindGameObjectWithTag("MainCamera");
            _cam = cam != null ? cam.transform : Camera.main != null ? Camera.main.transform : null;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (Input.GetKeyDown(KeyCode.Q)) Toggle();
            if (!Active) return;

            if (!_vitals.ConsumeBattery(batteryDrainPerSecond * Time.deltaTime))
            {
                Toggle(false);
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = scanInterval;
                ScanOnce();
            }
        }

        public void Toggle(bool? force = null)
        {
            Active = force ?? !Active;
            if (!Active) EventBus.Publish(new ScannerResult(null));
            else ScanOnce();
        }

        public string ScanOnce()
        {
            if (_cam == null) return null;
            return ScanAround(_cam.position, scanRange);
        }

        public string ScanAround(Vector3 origin, float range)
        {
            int n = Physics.OverlapSphereNonAlloc(origin, range, _hits, scanMask, QueryTriggerInteraction.Collide);
            var sb = new StringBuilder();
            int shown = 0;

            for (int i = 0; i < n && shown < 3; i++)
            {
                var go = _hits[i].gameObject;
                float dist = Vector3.Distance(origin, go.transform.position);
                string label = null;

                var pickup = FindInChain<PickupItem>(go);
                var door = FindInChain<DoorController>(go);
                var node = FindInChain<PowerNode>(go);
                var echo = FindInChain<EchoController>(go);

                if (pickup != null && !pickup.Collected) label = $"OBJECT SIGNATURE: {ItemName(pickup)}  {dist:F0}m";
                else if (node != null) label = node.Online ? "POWER NODE: ONLINE" : $"POWER NODE OFFLINE — needs fuse  {dist:F0}m";
                else if (door != null && door.NeedsAccess > 0 && !door.IsOpen) label = $"SEALED DOOR [LVL-{door.NeedsAccess}]  {dist:F0}m";
                else if (echo != null) label = $"BIOLOGICAL SIGNATURE DETECTED  {dist:F0}m — DO NOT RUN";

                if (label == null) continue;
                shown++;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(label);
            }

            string lines = sb.Length == 0 ? null : sb.ToString();
            EventBus.Publish(new ScannerResult(lines));
            return lines;
        }

        private static string ItemName(PickupItem p)
        {
            var def = p.ItemDef;
            return def != null ? def.displayName.ToUpper() : "UNKNOWN OBJECT";
        }

        private static T FindInChain<T>(GameObject go) where T : Component
        {
            var t = go.GetComponentInParent<T>();
            return t != null ? t : go.GetComponent<T>();
        }
    }
}
