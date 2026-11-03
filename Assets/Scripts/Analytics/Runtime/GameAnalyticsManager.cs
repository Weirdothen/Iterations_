using System;
using System.Collections.Generic;
using UnityEngine;
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;

namespace KajoGames.CopyPaste.Analytics
{
    /// <summary>
    /// Centralized analytics manager and facade for the entire project.
    /// Every tracked event passes through this manager before dispatching to GameAnalytics.
    /// Manages safe SDK initialization, validation, sanitization, pre-init queueing, and logging.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameAnalyticsManager : MonoBehaviour
    {
        public static GameAnalyticsManager Instance { get; private set; }

        /// <summary>
        /// Event invoked whenever an analytics event is successfully tracked.
        /// Useful for debug consoles, test runners, or local telemetry listeners.
        /// </summary>
        public static event Action<AnalyticsEventData> OnEventTracked;

        private static readonly Queue<AnalyticsEventData> _eventQueue = new Queue<AnalyticsEventData>();
        private static bool _isInitialized;
        private static bool _isInitializing;

        public static bool IsInitialized => _isInitialized || GameAnalytics.Initialized;

        #region Unity Lifecycle & Auto-Boot

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (AnalyticsSettings.Instance.AutoInitializeSDK)
            {
                EnsureInstance();
            }
        }

        /// <summary>
        /// Guarantees that a persistent GameAnalyticsManager exists in the scene without creating duplicates.
        /// </summary>
        public static GameAnalyticsManager EnsureInstance()
        {
            if (Instance != null) return Instance;

            // Check if one already exists in the scene
            GameAnalyticsManager existing = FindAnyObjectByType<GameAnalyticsManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            // Create persistent manager GameObject
            GameObject go = new GameObject("[AnalyticsManager]");
            Instance = go.AddComponent<GameAnalyticsManager>();
            DontDestroyOnLoad(go);
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSDK();
        }

        private void OnEnable()
        {
            GameAnalytics.onInitialize += HandleGameAnalyticsInitialized;
        }

        private void OnDisable()
        {
            GameAnalytics.onInitialize -= HandleGameAnalyticsInitialized;
        }

        #endregion

        #region Initialization Logic

        /// <summary>
        /// Safely initializes GameAnalytics SDK if not already initialized, avoiding duplicate initialization objects.
        /// </summary>
        public static void InitializeSDK()
        {
            if (_isInitialized || GameAnalytics.Initialized)
            {
                _isInitialized = true;
                FlushQueue();
                return;
            }

            if (_isInitializing) return;
            _isInitializing = true;

            EnsureGameAnalyticsObject();

            try
            {
                GameAnalytics.Initialize();
                _isInitialized = GameAnalytics.Initialized;

                if (AnalyticsSettings.Instance.EnableDebugLogging)
                {
                    Debug.Log($"[Analytics] GameAnalytics initialized successfully (Platform: {Application.platform}).");
                }

                FlushQueue();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Analytics] Failed to initialize GameAnalytics: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private static void EnsureGameAnalyticsObject()
        {
            // Verify whether a GameAnalytics GameObject already exists in the scene
            GameAnalytics gaComponent = FindAnyObjectByType<GameAnalytics>();
            if (gaComponent == null)
            {
                // Attach GameAnalytics component to the persistent manager GameObject to avoid separate objects
                if (Instance != null)
                {
                    Instance.gameObject.AddComponent<GameAnalytics>();
                }
                else
                {
                    GameObject gaGo = new GameObject("GameAnalytics");
                    gaGo.AddComponent<GameAnalytics>();
                    DontDestroyOnLoad(gaGo);
                }
            }
        }

        private void HandleGameAnalyticsInitialized(object sender, bool success)
        {
            _isInitialized = true;
            _isInitializing = false;

            if (AnalyticsSettings.Instance.EnableDebugLogging)
            {
                Debug.Log($"[Analytics] Received GameAnalytics.onInitialize event (Success: {success}). Flushing pre-init queue...");
            }

            FlushQueue();
        }

        private static void FlushQueue()
        {
            if (_eventQueue.Count == 0) return;

            int count = _eventQueue.Count;
            while (_eventQueue.Count > 0)
            {
                AnalyticsEventData evt = _eventQueue.Dequeue();
                DispatchEventToSDK(evt);
            }

            if (AnalyticsSettings.Instance.EnableDebugLogging)
            {
                Debug.Log($"[Analytics] Successfully flushed {count} queued events.");
            }
        }

        #endregion

        #region Core Direct API: Event(...)

        /// <summary>
        /// Track a simple event with category and action names (e.g. "combat", "weapon_fired").
        /// </summary>
        public static void Event(string category, string action)
        {
            Track(new AnalyticsEventData(category, action));
        }

        /// <summary>
        /// Track an event with category, action, and a numeric value (e.g. "combat", "damage_dealt", 25.5f).
        /// </summary>
        public static void Event(string category, string action, float value)
        {
            Track(new AnalyticsEventData(category, action, null, value));
        }

        /// <summary>
        /// Track an event with category, action, and label/target (e.g. "combat", "weapon_fired", "machine_gun").
        /// </summary>
        public static void Event(string category, string action, string label)
        {
            Track(new AnalyticsEventData(category, action, label));
        }

        /// <summary>
        /// Track an event with category, action, label, and numeric value.
        /// </summary>
        public static void Event(string category, string action, string label, float? value)
        {
            Track(new AnalyticsEventData(category, action, label, value));
        }

        /// <summary>
        /// Track an event with category, action, and custom metadata dictionary.
        /// </summary>
        public static void Event(string category, string action, IDictionary<string, object> customFields)
        {
            Track(new AnalyticsEventData(category, action, null, null, customFields));
        }

        /// <summary>
        /// Track an event with category, action, numeric value, and custom metadata dictionary.
        /// </summary>
        public static void Event(string category, string action, float value, IDictionary<string, object> customFields)
        {
            Track(new AnalyticsEventData(category, action, null, value, customFields));
        }

        /// <summary>
        /// Track an event with category, action, label, and custom metadata dictionary.
        /// </summary>
        public static void Event(string category, string action, string label, IDictionary<string, object> customFields)
        {
            Track(new AnalyticsEventData(category, action, label, null, customFields));
        }

        /// <summary>
        /// Track an event with category, action, label, numeric value, and custom metadata dictionary (5 arguments).
        /// </summary>
        public static void Event(string category, string action, string label, float? value, IDictionary<string, object> customFields)
        {
            Track(new AnalyticsEventData(category, action, label, value, customFields));
        }

        /// <summary>
        /// Track an event using the strongly-typed <see cref="AnalyticsEventCategory"/> enum.
        /// </summary>
        public static void Event(AnalyticsEventCategory category, string action)
        {
            Track(new AnalyticsEventData(category, action));
        }

        /// <summary>
        /// Track an event with typed category, action, and numeric value.
        /// </summary>
        public static void Event(AnalyticsEventCategory category, string action, float value)
        {
            Track(new AnalyticsEventData(category, action, null, value));
        }

        /// <summary>
        /// Track an event with typed category, action, and label.
        /// </summary>
        public static void Event(AnalyticsEventCategory category, string action, string label)
        {
            Track(new AnalyticsEventData(category, action, label));
        }

        /// <summary>
        /// Track an event with typed category, action, label, and numeric value.
        /// </summary>
        public static void Event(AnalyticsEventCategory category, string action, string label, float? value)
        {
            Track(new AnalyticsEventData(category, action, label, value));
        }

        /// <summary>
        /// Track an event with typed category, action, and custom metadata dictionary.
        /// </summary>
        public static void Event(AnalyticsEventCategory category, string action, IDictionary<string, object> customFields)
        {
            Track(new AnalyticsEventData(category, action, null, null, customFields));
        }

        /// <summary>
        /// Track an event with typed category, action, numeric value, and custom metadata dictionary.
        /// </summary>
        public static void Event(AnalyticsEventCategory category, string action, float value, IDictionary<string, object> customFields)
        {
            Track(new AnalyticsEventData(category, action, null, value, customFields));
        }

        /// <summary>
        /// Track an event with typed category, action, label, and custom metadata dictionary.
        /// </summary>
        public static void Event(AnalyticsEventCategory category, string action, string label, IDictionary<string, object> customFields)
        {
            Track(new AnalyticsEventData(category, action, label, null, customFields));
        }

        /// <summary>
        /// Track an event with typed category, action, label, numeric value, and custom metadata dictionary (5 arguments).
        /// </summary>
        public static void Event(AnalyticsEventCategory category, string action, string label, float? value, IDictionary<string, object> customFields)
        {
            Track(new AnalyticsEventData(category, action, label, value, customFields));
        }

        #endregion

        #region Typed Interface API: Track(...)

        /// <summary>
        /// Track a strongly-typed event implementing <see cref="IAnalyticsEvent"/>.
        /// </summary>
        public static void Track(IAnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null)
            {
                Debug.LogWarning("[Analytics] Track called with null IAnalyticsEvent.");
                return;
            }

            Track(analyticsEvent.ToEventData());
        }

        /// <summary>
        /// Track a fully formed <see cref="AnalyticsEventData"/> container.
        /// Every event in the system flows through this single entry point.
        /// </summary>
        public static void Track(AnalyticsEventData eventData)
        {
            if (eventData == null)
            {
                Debug.LogWarning("[Analytics] Track called with null AnalyticsEventData.");
                return;
            }

            AnalyticsSettings settings = AnalyticsSettings.Instance;

            // Master switch check
            if (!settings.EnableAnalytics)
            {
                return;
            }

            // Validation
            if (!eventData.Validate(out string errorReason))
            {
                if (settings.SanitizeEventNames)
                {
                    // Automatic repair if sanitization is enabled
                    eventData.Category = AnalyticsEventData.SanitizeSegment(eventData.Category);
                    eventData.Action = AnalyticsEventData.SanitizeSegment(eventData.Action);
                    if (!string.IsNullOrEmpty(eventData.Label))
                    {
                        eventData.Label = AnalyticsEventData.SanitizeSegment(eventData.Label);
                    }
                }
                else
                {
                    Debug.LogWarning($"[Analytics] Dropped invalid event '{eventData.Category}:{eventData.Action}': {errorReason}");
                    return;
                }
            }

            // SDK Initialization check / pre-init queueing
            if (!IsInitialized)
            {
                if (settings.QueueEventsUntilInitialized)
                {
                    if (_eventQueue.Count < settings.MaxQueuedEvents)
                    {
                        _eventQueue.Enqueue(eventData);
                        if (settings.EnableDebugLogging)
                        {
                            Debug.Log($"[Analytics] Queued event before SDK initialization: {eventData.ToFormattedEventId()} (Queue count: {_eventQueue.Count})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Analytics] Pre-initialization queue full ({settings.MaxQueuedEvents}). Dropping event '{eventData.ToFormattedEventId()}'.");
                    }

                    EnsureInstance();
                    return;
                }
                else
                {
                    // Try to initialize on-demand
                    InitializeSDK();
                }
            }

            DispatchEventToSDK(eventData);
        }

        private static void DispatchEventToSDK(AnalyticsEventData eventData)
        {
            AnalyticsSettings settings = AnalyticsSettings.Instance;
            string eventId = eventData.ToFormattedEventId(settings.SanitizeEventNames);

            try
            {
                if (eventData.Value.HasValue)
                {
                    if (eventData.CustomFields != null && eventData.CustomFields.Count > 0)
                    {
                        GameAnalytics.NewDesignEvent(eventId, eventData.Value.Value, eventData.CustomFields);
                    }
                    else
                    {
                        GameAnalytics.NewDesignEvent(eventId, eventData.Value.Value);
                    }
                }
                else
                {
                    if (eventData.CustomFields != null && eventData.CustomFields.Count > 0)
                    {
                        GameAnalytics.NewDesignEvent(eventId, eventData.CustomFields);
                    }
                    else
                    {
                        GameAnalytics.NewDesignEvent(eventId);
                    }
                }

                if (settings.EnableDebugLogging)
                {
                    string valueStr = eventData.Value.HasValue ? $" | Value: {eventData.Value.Value}" : "";
                    string fieldsStr = (eventData.CustomFields != null && eventData.CustomFields.Count > 0)
                        ? $" | Fields: {eventData.CustomFields.Count}" : "";
                    Debug.Log($"[Analytics Event] {eventId}{valueStr}{fieldsStr}");
                }

                OnEventTracked?.Invoke(eventData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Analytics] Error dispatching event '{eventId}' to GameAnalytics: {ex.Message}");
            }
        }

        #endregion

        #region High-Level Progression, Economy, and Error Routing

        /// <summary>
        /// Track player progression directly mapped to GameAnalytics progression events.
        /// </summary>
        public static void TrackProgression(GAProgressionStatus status, string progression01, string progression02 = null, string progression03 = null, int? score = null, IDictionary<string, object> customFields = null)
        {
            if (!AnalyticsSettings.Instance.EnableAnalytics) return;
            if (!IsInitialized) InitializeSDK();

            try
            {
                if (score.HasValue)
                {
                    if (string.IsNullOrEmpty(progression02))
                    {
                        if (customFields != null)
                            GameAnalytics.NewProgressionEvent(status, progression01, score.Value, customFields);
                        else
                            GameAnalytics.NewProgressionEvent(status, progression01, score.Value);
                    }
                    else if (string.IsNullOrEmpty(progression03))
                    {
                        if (customFields != null)
                            GameAnalytics.NewProgressionEvent(status, progression01, progression02, score.Value, customFields);
                        else
                            GameAnalytics.NewProgressionEvent(status, progression01, progression02, score.Value);
                    }
                    else
                    {
                        if (customFields != null)
                            GameAnalytics.NewProgressionEvent(status, progression01, progression02, progression03, score.Value, customFields);
                        else
                            GameAnalytics.NewProgressionEvent(status, progression01, progression02, progression03, score.Value);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(progression02))
                    {
                        if (customFields != null)
                            GameAnalytics.NewProgressionEvent(status, progression01, customFields);
                        else
                            GameAnalytics.NewProgressionEvent(status, progression01);
                    }
                    else if (string.IsNullOrEmpty(progression03))
                    {
                        if (customFields != null)
                            GameAnalytics.NewProgressionEvent(status, progression01, progression02, customFields);
                        else
                            GameAnalytics.NewProgressionEvent(status, progression01, progression02);
                    }
                    else
                    {
                        if (customFields != null)
                            GameAnalytics.NewProgressionEvent(status, progression01, progression02, progression03, customFields);
                        else
                            GameAnalytics.NewProgressionEvent(status, progression01, progression02, progression03);
                    }
                }

                if (AnalyticsSettings.Instance.EnableDebugLogging)
                {
                    string scoreStr = score.HasValue ? $" | Score: {score.Value}" : "";
                    Debug.Log($"[Analytics Progression] {status} - {progression01}/{progression02}/{progression03}{scoreStr}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Analytics] Error tracking progression event: {ex.Message}");
            }
        }

        /// <summary>
        /// Track in-game virtual economy flow mapped to GameAnalytics resource events.
        /// </summary>
        public static void TrackResource(GAResourceFlowType flowType, string currency, float amount, string itemType, string itemId, IDictionary<string, object> customFields = null)
        {
            if (!AnalyticsSettings.Instance.EnableAnalytics) return;
            if (!IsInitialized) InitializeSDK();

            try
            {
                if (customFields != null)
                {
                    GameAnalytics.NewResourceEvent(flowType, currency, amount, itemType, itemId, customFields);
                }
                else
                {
                    GameAnalytics.NewResourceEvent(flowType, currency, amount, itemType, itemId);
                }

                if (AnalyticsSettings.Instance.EnableDebugLogging)
                {
                    Debug.Log($"[Analytics Resource] {flowType} - {amount} {currency} ({itemType}/{itemId})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Analytics] Error tracking resource event: {ex.Message}");
            }
        }

        /// <summary>
        /// Track custom error events mapped to GameAnalytics error severity levels.
        /// </summary>
        public static void TrackError(GAErrorSeverity severity, string message, IDictionary<string, object> customFields = null)
        {
            if (!AnalyticsSettings.Instance.EnableAnalytics) return;
            if (!IsInitialized) InitializeSDK();

            try
            {
                if (customFields != null)
                {
                    GameAnalytics.NewErrorEvent(severity, message, customFields);
                }
                else
                {
                    GameAnalytics.NewErrorEvent(severity, message);
                }

                if (AnalyticsSettings.Instance.EnableDebugLogging)
                {
                    Debug.Log($"[Analytics Error] {severity} - {message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Analytics] Error tracking error event: {ex.Message}");
            }
        }

        #endregion

        #region User Identity & Dimensions

        /// <summary>
        /// Sets a custom user identifier (e.g. Steam ID, Account ID).
        /// </summary>
        public static void SetUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;
            GameAnalytics.SetCustomId(userId);
        }

        /// <summary>
        /// Sets the first custom dimension (must match pre-configured values if defined in GA Settings).
        /// </summary>
        public static void SetCustomDimension01(string dimension)
        {
            GameAnalytics.SetCustomDimension01(dimension);
        }

        /// <summary>
        /// Sets the second custom dimension.
        /// </summary>
        public static void SetCustomDimension02(string dimension)
        {
            GameAnalytics.SetCustomDimension02(dimension);
        }

        /// <summary>
        /// Sets the third custom dimension.
        /// </summary>
        public static void SetCustomDimension03(string dimension)
        {
            GameAnalytics.SetCustomDimension03(dimension);
        }

        #endregion
    }
}
