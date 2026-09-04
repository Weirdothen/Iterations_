using UnityEngine;
using System;

namespace Iterations.Events
{
    [CreateAssetMenu(menuName = "Events/String Event Channel")]
    public class StringEventChannelSO : ScriptableObject
    {
        public event Action<string> OnEventRaised;

        public void RaiseEvent(string value)
        {
            OnEventRaised?.Invoke(value);
        }
    }
}