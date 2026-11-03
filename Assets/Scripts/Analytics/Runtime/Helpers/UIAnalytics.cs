using System.Collections.Generic;

namespace KajoGames.CopyPaste.Analytics.Helpers
{
    /// <summary>
    /// High-level domain helper for user interface tracking: button presses,
    /// menu navigation, panel switches, settings changes, and external links.
    /// </summary>
    public static class UIAnalytics
    {
        /// <summary>
        /// Track when any UI button is pressed.
        /// </summary>
        public static void TrackButtonClicked(string buttonName, string screenName = "main_menu")
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.UI, "button_click", $"{screenName}:{buttonName}");
        }

        /// <summary>
        /// Track when a UI panel or menu screen is opened.
        /// </summary>
        public static void TrackPanelOpened(string panelName)
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.UI, "panel_opened", panelName);
        }

        /// <summary>
        /// Track when a UI panel or modal is closed.
        /// </summary>
        public static void TrackPanelClosed(string panelName)
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.UI, "panel_closed", panelName);
        }

        /// <summary>
        /// Track settings modification (e.g. resolution, quality, fullscreen).
        /// </summary>
        public static void TrackSettingChanged(string settingName, object newValue)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "value", newValue != null ? newValue.ToString() : "null" }
            };
            GameAnalyticsManager.Event(AnalyticsEventCategory.Settings, "setting_changed", settingName, null, fields);
        }

        /// <summary>
        /// Track volume adjustments on audio channels.
        /// </summary>
        public static void TrackVolumeChanged(string audioChannel, float volumeNormalized)
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.Settings, "volume_changed", audioChannel, volumeNormalized);
        }

        /// <summary>
        /// Track when an external URL or community button is opened.
        /// </summary>
        public static void TrackUrlOpened(string urlLabel, string targetUrl = "")
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "url", targetUrl }
            };
            GameAnalyticsManager.Event(AnalyticsEventCategory.UI, "url_opened", urlLabel, null, fields);
        }

        /// <summary>
        /// Track multiplayer lobby and connection flows (e.g. host, join, connect_failed).
        /// </summary>
        public static void TrackMultiplayerFlow(string action, string details = "")
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.UI, "multiplayer_flow", action);
        }
    }
}
