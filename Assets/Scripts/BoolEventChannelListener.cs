using UnityEngine;
using UnityEngine.Events;
using Iterations.Events;

namespace Iterations.Events
{
    public class BoolEventChannelListener : MonoBehaviour
    {
        [SerializeField] private BoolEventChannelSO eventChannel;
        [SerializeField] private UnityEvent<bool> response;

        private void OnEnable()
        {
            if (eventChannel != null)
                eventChannel.OnEventRaised += Respond;
        }

        private void OnDisable()
        {
            if (eventChannel != null)
                eventChannel.OnEventRaised -= Respond;
        }

        private void Respond(bool value)
        {
            response?.Invoke(value);
        }
    }
}