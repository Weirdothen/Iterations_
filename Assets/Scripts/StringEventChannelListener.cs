using UnityEngine;
using UnityEngine.Events;
using Iterations.Events;

namespace Iterations.Events
{
    public class StringEventChannelListener : MonoBehaviour
    {
        [SerializeField] private StringEventChannelSO eventChannel;
        [SerializeField] private UnityEvent<string> response;

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

        private void Respond(string value)
        {
            response?.Invoke(value);
        }
    }
}