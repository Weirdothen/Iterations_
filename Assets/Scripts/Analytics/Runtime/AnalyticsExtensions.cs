using UnityEngine;
using UnityEngine.UI;

namespace KajoGames.CopyPaste.Analytics
{
    /// <summary>
    /// Extension methods providing convenient, idiomatic analytics tracking
    /// for Unity UI controls, Components, and GameObjects.
    /// </summary>
    public static class AnalyticsExtensions
    {
        #region UI Button Extensions

        /// <summary>
        /// Binds an analytics tracking call to a UI Button's onClick event with category "ui".
        /// </summary>
        public static Button TrackOnClick(this Button button, string action, string screenName = null)
        {
            if (button == null) return button;

            button.onClick.AddListener(() =>
            {
                GameAnalyticsManager.Event(AnalyticsEventCategory.UI, action, screenName);
            });

            return button;
        }

        /// <summary>
        /// Binds an analytics tracking call to a UI Button's onClick event with a custom category.
        /// </summary>
        public static Button TrackOnClick(this Button button, AnalyticsEventCategory category, string action, string label = null)
        {
            if (button == null) return button;

            button.onClick.AddListener(() =>
            {
                GameAnalyticsManager.Event(category, action, label);
            });

            return button;
        }

        #endregion

        #region UI Slider & Toggle Extensions

        /// <summary>
        /// Binds an analytics tracking call to a UI Slider's onValueChanged event.
        /// </summary>
        public static Slider TrackOnValueChanged(this Slider slider, string settingName)
        {
            if (slider == null) return slider;

            slider.onValueChanged.AddListener(value =>
            {
                GameAnalyticsManager.Event(AnalyticsEventCategory.Settings, "slider_changed", settingName, value);
            });

            return slider;
        }

        /// <summary>
        /// Binds an analytics tracking call to a UI Toggle's onValueChanged event.
        /// </summary>
        public static Toggle TrackOnValueChanged(this Toggle toggle, string settingName)
        {
            if (toggle == null) return toggle;

            toggle.onValueChanged.AddListener(isOn =>
            {
                GameAnalyticsManager.Event(AnalyticsEventCategory.Settings, "toggle_changed", settingName, isOn ? 1f : 0f);
            });

            return toggle;
        }

        #endregion

        #region Component & GameObject Extensions

        /// <summary>
        /// Directly track an analytics event from any MonoBehaviour or Component.
        /// </summary>
        public static void TrackAnalyticsEvent(this Component component, string category, string action, float? value = null)
        {
            if (component == null) return;
            if (value.HasValue)
            {
                GameAnalyticsManager.Event(category, action, value.Value);
            }
            else
            {
                GameAnalyticsManager.Event(category, action);
            }
        }

        /// <summary>
        /// Directly track an analytics event from any MonoBehaviour or Component using typed category.
        /// </summary>
        public static void TrackAnalyticsEvent(this Component component, AnalyticsEventCategory category, string action, float? value = null)
        {
            if (component == null) return;
            if (value.HasValue)
            {
                GameAnalyticsManager.Event(category, action, value.Value);
            }
            else
            {
                GameAnalyticsManager.Event(category, action);
            }
        }

        /// <summary>
        /// Directly track an analytics event from any GameObject.
        /// </summary>
        public static void TrackAnalyticsEvent(this GameObject gameObject, string category, string action, float? value = null)
        {
            if (gameObject == null) return;
            if (value.HasValue)
            {
                GameAnalyticsManager.Event(category, action, value.Value);
            }
            else
            {
                GameAnalyticsManager.Event(category, action);
            }
        }

        #endregion
    }
}
