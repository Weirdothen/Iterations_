using System;
using System.Collections.Generic;
using UnityEngine;

namespace KajoGames.CopyPaste.Analytics
{
    /// <summary>
    /// Reusable component for configuring and triggering analytics events directly
    /// from the Unity Inspector without writing any code. Ideal for level designers,
    /// artists, and UI button wiring.
    /// </summary>
    [AddComponentMenu("Kajo Games/Analytics/Analytics Tracker")]
    public class AnalyticsTracker : MonoBehaviour
    {
        public enum TriggerMode
        {
            ManualOrEvent,
            Awake,
            Start,
            OnEnable,
            OnDisable,
            OnDestroy,
            OnTriggerEnter2D,
            OnTriggerExit2D,
            OnCollisionEnter2D
        }

        [Serializable]
        public struct CustomFieldEntry
        {
            public string key;
            public string stringValue;
        }

        [Header("Trigger Configuration")]
        [Tooltip("When this analytics event should automatically fire.")]
        [SerializeField] private TriggerMode triggerOn = TriggerMode.ManualOrEvent;

        [Tooltip("If true, this event will only fire once during this object's lifetime.")]
        [SerializeField] private bool triggerOnce = false;

        [Tooltip("For 2D physics triggers, only fire if the colliding GameObject has this tag (leave empty for any).")]
        [SerializeField] private string targetTag = "Player";

        [Header("Event Definition")]
        [Tooltip("Domain category for the event.")]
        [SerializeField] private AnalyticsEventCategory category = AnalyticsEventCategory.Gameplay;

        [Tooltip("Action or primary identifier (e.g. 'checkpoint_reached', 'chest_opened').")]
        [SerializeField] private string action = "custom_action";

        [Tooltip("Optional context, sub-category, or label (e.g. 'level_01', 'main_menu').")]
        [SerializeField] private string label = "";

        [Header("Value & Telemetry")]
        [Tooltip("Whether to include a numeric value with this event.")]
        [SerializeField] private bool includeValue = false;

        [Tooltip("Static numeric value to report (can be overridden dynamically via TriggerWithValue).")]
        [SerializeField] private float numericValue = 0f;

        [Tooltip("Optional custom key-value metadata fields attached to this event.")]
        [SerializeField] private List<CustomFieldEntry> customFields = new List<CustomFieldEntry>();

        private bool _hasTriggered;

        #region Unity Lifecycle Triggers

        private void Awake()
        {
            if (triggerOn == TriggerMode.Awake) Trigger();
        }

        private void Start()
        {
            if (triggerOn == TriggerMode.Start) Trigger();
        }

        private void OnEnable()
        {
            if (triggerOn == TriggerMode.OnEnable) Trigger();
        }

        private void OnDisable()
        {
            if (triggerOn == TriggerMode.OnDisable) Trigger();
        }

        private void OnDestroy()
        {
            if (triggerOn == TriggerMode.OnDestroy) Trigger();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerOn != TriggerMode.OnTriggerEnter2D) return;
            if (MatchesTag(other.gameObject))
            {
                Trigger();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (triggerOn != TriggerMode.OnTriggerExit2D) return;
            if (MatchesTag(other.gameObject))
            {
                Trigger();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (triggerOn != TriggerMode.OnCollisionEnter2D) return;
            if (MatchesTag(collision.gameObject))
            {
                Trigger();
            }
        }

        private bool MatchesTag(GameObject obj)
        {
            if (string.IsNullOrEmpty(targetTag)) return true;
            return obj.CompareTag(targetTag);
        }

        #endregion

        #region Public Dispatch Methods for Inspector & UnityEvents

        /// <summary>
        /// Trigger the configured analytics event. Ideal target for Button.onClick UnityEvents.
        /// </summary>
        public void Trigger()
        {
            if (triggerOnce && _hasTriggered) return;

            float? val = includeValue ? (float?)numericValue : null;
            Dispatch(action, label, val);
            _hasTriggered = true;
        }

        /// <summary>
        /// Trigger the event with an overriding dynamic numeric value (e.g. from a UI Slider or health metric).
        /// </summary>
        public void TriggerWithValue(float dynamicValue)
        {
            if (triggerOnce && _hasTriggered) return;

            Dispatch(action, label, dynamicValue);
            _hasTriggered = true;
        }

        /// <summary>
        /// Trigger the event with an overriding dynamic action string.
        /// </summary>
        public void TriggerWithAction(string dynamicAction)
        {
            if (triggerOnce && _hasTriggered) return;

            float? val = includeValue ? (float?)numericValue : null;
            Dispatch(dynamicAction, label, val);
            _hasTriggered = true;
        }

        /// <summary>
        /// Trigger the event with dynamic action and dynamic numeric value.
        /// </summary>
        public void TriggerWithActionAndValue(string dynamicAction, float dynamicValue)
        {
            if (triggerOnce && _hasTriggered) return;

            Dispatch(dynamicAction, label, dynamicValue);
            _hasTriggered = true;
        }

        /// <summary>
        /// Resets the trigger-once state, allowing the event to fire again.
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
        }

        private void Dispatch(string resolvedAction, string resolvedLabel, float? resolvedValue)
        {
            Dictionary<string, object> fieldsDict = null;
            if (customFields != null && customFields.Count > 0)
            {
                fieldsDict = new Dictionary<string, object>();
                for (int i = 0; i < customFields.Count; i++)
                {
                    if (!string.IsNullOrEmpty(customFields[i].key))
                    {
                        fieldsDict[customFields[i].key] = customFields[i].stringValue;
                    }
                }
            }

            AnalyticsEventData data = new AnalyticsEventData(
                category,
                resolvedAction,
                string.IsNullOrEmpty(resolvedLabel) ? null : resolvedLabel,
                resolvedValue,
                fieldsDict
            );

            GameAnalyticsManager.Track(data);
        }

        #endregion
    }
}
