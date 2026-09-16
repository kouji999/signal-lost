using SignalLost.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SignalLost.UI
{
    public class InventoryPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private Text content;

        private Inventory.Inventory _inv;
        private bool _open;

        public bool IsOpen => _open;

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) _inv = player.GetComponent<Inventory.Inventory>();
            if (panel != null) panel.alpha = 0f;
            EventBus.Subscribe<InventoryChanged>(OnChanged);
            EventBus.Subscribe<GameStateChanged>(OnStateChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<InventoryChanged>(OnChanged);
            EventBus.Unsubscribe<GameStateChanged>(OnStateChanged);
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (Input.GetKeyDown(KeyCode.Tab) &&
                (GameManager.Instance.State == GameState.Playing || _open))
            {
                if (_open) Close();
                else Open();
            }
        }

        public void Open()
        {
            _open = true;
            GameManager.Instance.SetState(GameState.UiOpen);
            if (panel != null) panel.alpha = 1f;
            Refresh();
        }

        public void Close()
        {
            _open = false;
            if (panel != null) panel.alpha = 0f;
            if (GameManager.Instance.State == GameState.UiOpen)
                GameManager.Instance.SetState(GameState.Playing);
        }

        private void OnStateChanged(GameStateChanged evt)
        {
            if (_open && evt.State != GameState.UiOpen)
            {
                _open = false;
                if (panel != null) panel.alpha = 0f;
            }
        }

        private void OnChanged(InventoryChanged evt)
        {
            if (_open) Refresh();
        }

        private void Refresh()
        {
            if (content == null || _inv == null) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>INVENTORY</b>  <color=#6b727a>[TAB] close   [1]-[6] use</color>");
            if (_inv.Items.Count == 0 && _inv.SlotCount == 0)
            {
                sb.Append("<color=#6b727a>Nothing carried.</color>");
            }
            else
            {
                var slots = _inv.SlotInfos();
                for (int i = 0; i < slots.Count; i++)
                {
                    var item = slots[i].def;
                    int count = slots[i].count;
                    string tag = item.kind == Inventory.ItemKind.Keycard
                        ? $"<color=#6b727a>pass lvl {item.accessLevel}</color>"
                        : EffectTag(item);
                    var c = ColorBlockToHex(item.uiColor);
                    string xs = count > 1 ? $" <color=#c9a13f>x{count}</color>" : "";
                    sb.AppendLine($"<color={c}>{i + 1}.</color> {item.displayName}{xs}  {tag}");
                    if (!string.IsNullOrEmpty(item.description))
                        sb.AppendLine($"    <size=11><color=#8d949d>{item.description}</color></size>");
                }
            }
            content.text = sb.ToString();
        }

        private static string EffectTag(Inventory.ItemDefinition item)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (item.healAmount > 0f) parts.Add($"+{item.healAmount:F0} HP");
            if (item.oxygenAmount > 0f) parts.Add($"+{item.oxygenAmount:F0} O2");
            if (item.batteryCharge > 0f) parts.Add($"+{item.batteryCharge:F0} PWR");
            return parts.Count == 0 ? "" : $"<color=#7fd1a0>{string.Join(" ", parts)}</color>";
        }

        private static string ColorBlockToHex(Color c)
        {
            Color g = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b));
            return ColorUtility.ToHtmlStringRGB(g);
        }
    }
}
