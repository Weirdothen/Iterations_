using UnityEngine;
using UnityEngine.Events;
using Iterations.Events;

public class IntEventChannelListener : MonoBehaviour
{
    [Header("Listen to Event Channels")]
    [Tooltip("The Int Event Channel to listen to.")]
    [SerializeField] private IntEventChannelSO eventChannel;

    [Header("Response")]
    [Tooltip("What happens when the event is raised.")]
    [SerializeField] private UnityEvent<int> response;

    private void OnEnable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnEventRaised += Respond;
        }
    }

    private void OnDisable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnEventRaised -= Respond;
        }
    }

    private void Respond(int value)
    {
        response?.Invoke(value);
    }
}