using System;

namespace KajoGames.CopyPaste.Analytics
{
    /// <summary>
    /// High-level event categories for organizing analytics events across the game.
    /// Used by GameAnalyticsManager, AnalyticsTracker, and typed event definitions.
    /// </summary>
    public enum AnalyticsEventCategory
    {
        Gameplay,
        Combat,
        Progression,
        Economy,
        UI,
        Session,
        Settings,
        Tutorial,
        Error,
        Custom
    }

    /// <summary>
    /// Utility methods for <see cref="AnalyticsEventCategory"/>.
    /// </summary>
    public static class AnalyticsEventCategoryExtensions
    {
        /// <summary>
        /// Converts the category enum into a standardized lowercase string for GameAnalytics hierarchy.
        /// </summary>
        public static string ToCategoryString(this AnalyticsEventCategory category)
        {
            switch (category)
            {
                case AnalyticsEventCategory.Gameplay: return "gameplay";
                case AnalyticsEventCategory.Combat: return "combat";
                case AnalyticsEventCategory.Progression: return "progression";
                case AnalyticsEventCategory.Economy: return "economy";
                case AnalyticsEventCategory.UI: return "ui";
                case AnalyticsEventCategory.Session: return "session";
                case AnalyticsEventCategory.Settings: return "settings";
                case AnalyticsEventCategory.Tutorial: return "tutorial";
                case AnalyticsEventCategory.Error: return "error";
                case AnalyticsEventCategory.Custom: return "custom";
                default: return category.ToString().ToLowerInvariant();
            }
        }
    }
}
