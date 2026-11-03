using UnityEngine;

namespace KajoGames.CopyPaste.Analytics
{
    /// <summary>
    /// Global configuration settings for the analytics framework.
    /// Can be created in Resources/Analytics/AnalyticsSettings.asset or loaded dynamically.
    /// </summary>
    [CreateAssetMenu(fileName = "AnalyticsSettings", menuName = "Kajo Games/Analytics/Settings")]
    public class AnalyticsSettings : ScriptableObject
    {
        public const string ResourcePath = "Analytics/AnalyticsSettings";

        [Header("Master Controls")]
        [Tooltip("Master switch for analytics tracking. When disabled, all event submissions are safely bypassed.")]
        [SerializeField] private bool enableAnalytics = true;

        [Tooltip("When true, automatically ensures GameAnalytics is initialized at game launch.")]
        [SerializeField] private bool autoInitializeSDK = true;

        [Header("Diagnostics & Debugging")]
        [Tooltip("Log formatted analytics events to the Unity console.")]
        [SerializeField] private bool enableDebugLogging = true;

        [Tooltip("In the Unity Editor, simulate events to console without raising native SDK warnings if keys are unconfigured.")]
        [SerializeField] private bool enableEditorSimulation = true;

        [Header("Validation & Safety")]
        [Tooltip("Automatically sanitize event strings to conform to GameAnalytics character and length restrictions.")]
        [SerializeField] private bool sanitizeEventNames = true;

        [Tooltip("Queue events dispatched before the SDK completes initialization, flushing once ready.")]
        [SerializeField] private bool queueEventsUntilInitialized = true;

        [Tooltip("Maximum number of events to retain in the pre-initialization queue.")]
        [SerializeField] private int maxQueuedEvents = 100;

        #region Properties

        public bool EnableAnalytics
        {
            get => enableAnalytics;
            set => enableAnalytics = value;
        }

        public bool AutoInitializeSDK
        {
            get => autoInitializeSDK;
            set => autoInitializeSDK = value;
        }

        public bool EnableDebugLogging
        {
            get => enableDebugLogging;
            set => enableDebugLogging = value;
        }

        public bool EnableEditorSimulation
        {
            get => enableEditorSimulation;
            set => enableEditorSimulation = value;
        }

        public bool SanitizeEventNames
        {
            get => sanitizeEventNames;
            set => sanitizeEventNames = value;
        }

        public bool QueueEventsUntilInitialized
        {
            get => queueEventsUntilInitialized;
            set => queueEventsUntilInitialized = value;
        }

        public int MaxQueuedEvents
        {
            get => maxQueuedEvents;
            set => maxQueuedEvents = Mathf.Max(1, value);
        }

        #endregion

        #region Singleton Accessor

        private static AnalyticsSettings _instance;

        public static AnalyticsSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<AnalyticsSettings>(ResourcePath);

                    if (_instance == null)
                    {
                        // Fallback runtime instance if asset is not yet created in Resources
                        _instance = CreateInstance<AnalyticsSettings>();
                        _instance.name = "AnalyticsSettings (Runtime Fallback)";
                    }
                }
                return _instance;
            }
        }

        #endregion
    }
}
