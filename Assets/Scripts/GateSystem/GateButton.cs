using UnityEngine;
using Iterations.Events;

namespace Iterations.Mechanics
{
    [RequireComponent(typeof(Collider2D))]
    public class GateButton : MonoBehaviour
    {
        [SerializeField] private LayerMask activatorLayers;
        [SerializeField] private VoidEventChannelSO onGateShouldOpen;
        [SerializeField] private VoidEventChannelSO onGateShouldClose;

        private int _occupantCount;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsValid(other)) return;

            _occupantCount++;

            if (_occupantCount == 1)
                onGateShouldOpen?.RaiseEvent();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsValid(other)) return;

            _occupantCount = Mathf.Max(0, _occupantCount - 1);

            if (_occupantCount == 0)
                onGateShouldClose?.RaiseEvent();
        }

        private bool IsValid(Collider2D other)
        {
            return (activatorLayers.value & (1 << other.gameObject.layer)) != 0;
        }
    }
}