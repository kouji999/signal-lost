using UnityEngine;

namespace SignalLost.Interaction
{
    public class PowerCellSlot : MonoBehaviour
    {
        [SerializeField] private GameObject cellVisual;

        public bool Filled { get; private set; }

        public void Fill()
        {
            Filled = true;
            if (cellVisual != null) cellVisual.SetActive(true);
        }
    }
}
