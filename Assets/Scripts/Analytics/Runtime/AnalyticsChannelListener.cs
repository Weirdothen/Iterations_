using UnityEngine;
using Iterations.Events;

namespace KajoGames.CopyPaste.Analytics
{
    /// <summary>
    /// Bridges ScriptableObject Event Channels (VoidEventChannelSO and IntEventChannelSO)
    /// to GameAnalytics events. Enables no-code analytics binding across scenes and prefabs.
    /// </summary>
    [AddComponentMenu("Kajo Games/Analytics/Analytics Channel Listener")]
    public class AnalyticsChannelListener : MonoBehaviour
    {
        [Header("Event Channels")]
        [Tooltip("Optional void event channel to listen to.")]
        [SerializeField] private VoidEventChannelSO voidChannel;

        [Tooltip("Optional int event channel to listen to (passes the int payload as the event numeric value).")]
        [SerializeField] private IntEventChannelSO intChannel;

        [Header("Analytics Configuration")]
        [SerializeField] private AnalyticsEventCategory category = AnalyticsEventCategory.Gameplay;
        [SerializeField] private string action = "channel_event";
        [SerializeField] private string label = "";
        [SerializeField] private bool includeValue = false;
        [SerializeField] private float defaultValue = 0f;

        private void OnEnable()
        {
            if (voidChannel != null) voidChannel.OnEventRaised += HandleVoidRaised;
            if (intChannel != null) intChannel.OnEventRaised += HandleIntRaised;
        }

        private void OnDisable()
        {
            if (voidChannel != null) voidChannel.OnEventRaised -= HandleVoidRaised;
            if (intChannel != null) intChannel.OnEventRaised -= HandleIntRaised;
        }

        private void HandleVoidRaised()
        {
            string resolvedLabel = string.IsNullOrEmpty(label) ? null : label;
            if (includeValue)
            {
                GameAnalyticsManager.Event(category, action, resolvedLabel, defaultValue);
            }
            else
            {
                GameAnalyticsManager.Event(category, action, resolvedLabel);
            }
        }

        private void HandleIntRaised(int value)
        {
            string resolvedLabel = string.IsNullOrEmpty(label) ? null : label;
            GameAnalyticsManager.Event(category, action, resolvedLabel, (float)value);
        }
    }
}
