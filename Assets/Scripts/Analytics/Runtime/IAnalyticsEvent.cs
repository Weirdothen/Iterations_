using System.Collections.Generic;

namespace KajoGames.CopyPaste.Analytics
{
    /// <summary>
    /// Interface for strongly-typed analytics events. Allows developers to define
    /// explicit structs or classes for complex events with compile-time safety.
    /// </summary>
    public interface IAnalyticsEvent
    {
        /// <summary>
        /// Domain category of the event.
        /// </summary>
        AnalyticsEventCategory Category { get; }

        /// <summary>
        /// Specific action or event identifier (e.g. "weapon_fired", "level_start").
        /// </summary>
        string Action { get; }

        /// <summary>
        /// Optional sub-category, target, or context descriptor (e.g. "machine_gun", "pause_menu").
        /// </summary>
        string Label { get; }

        /// <summary>
        /// Optional numeric value associated with the event (e.g. damage, duration, score).
        /// </summary>
        float? Value { get; }

        /// <summary>
        /// Optional custom fields / dimensions dictionary for rich telemetry export.
        /// </summary>
        IDictionary<string, object> CustomFields { get; }

        /// <summary>
        /// Converts this typed event into a normalized <see cref="AnalyticsEventData"/> container.
        /// </summary>
        AnalyticsEventData ToEventData();
    }
}
