using Unity.VisualScripting;
using UnityEngine;

namespace Iterations.Mechanics
{
    [RequireComponent(typeof(Collider2D))]
    public class GateButton : MonoBehaviour
    {
        [SerializeField] private LayerMask activatorLayers;
        [SerializeField] private Gate[] targetGates;

        private int _occupantCount;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsValid(other)) return;

            _occupantCount++;

            if (_occupantCount == 1)
                SetGates(open: true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsValid(other)) return;

            _occupantCount = Mathf.Max(0, _occupantCount - 1);

            if (_occupantCount == 0)
                SetGates(open: false);
        }

        private void SetGates(bool open)
        {
            foreach (var gate in targetGates)
            {
                if (gate == null) continue;
                if (open) gate.Open();
                else gate.Close();
            }
        }

        private bool IsValid(Collider2D other)
        {
            return (activatorLayers.value & (1 << other.gameObject.layer)) != 0;
        }
    }
}