using System.Collections.Generic;
using GameAnalyticsSDK;

namespace KajoGames.CopyPaste.Analytics.Helpers
{
    /// <summary>
    /// High-level domain helper for in-game economy, virtual currency flow (source/sink),
    /// item unlocks, cosmetic acquisitions, and collectible rewards.
    /// Integrated with GameAnalytics Resource events.
    /// </summary>
    public static class EconomyAnalytics
    {
        public const string DefaultCurrency = "Coins";

        /// <summary>
        /// Track currency or resource gained (Source).
        /// </summary>
        public static void TrackResourceGained(string currency, float amount, string itemType, string itemId)
        {
            // Native GameAnalytics Resource Event
            GameAnalyticsManager.TrackResource(GAResourceFlowType.Source, currency, amount, itemType, itemId);

            // Parallel Design Event
            GameAnalyticsManager.Event(AnalyticsEventCategory.Economy, "resource_source", $"{currency}:{itemType}:{itemId}", amount);
        }

        /// <summary>
        /// Track currency or resource spent (Sink).
        /// </summary>
        public static void TrackResourceSpent(string currency, float amount, string itemType, string itemId)
        {
            // Native GameAnalytics Resource Event
            GameAnalyticsManager.TrackResource(GAResourceFlowType.Sink, currency, amount, itemType, itemId);

            // Parallel Design Event
            GameAnalyticsManager.Event(AnalyticsEventCategory.Economy, "resource_sink", $"{currency}:{itemType}:{itemId}", amount);
        }

        /// <summary>
        /// Track when an in-game item or upgrade is purchased/unlocked.
        /// </summary>
        public static void TrackItemPurchased(string itemId, int cost, string currency = DefaultCurrency)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "cost", cost },
                { "currency", currency }
            };

            TrackResourceSpent(currency, cost, "Purchase", itemId);
            GameAnalyticsManager.Event(AnalyticsEventCategory.Economy, "item_purchased", itemId, cost, fields);
        }

        /// <summary>
        /// Track rewards received from level pickups or achievements.
        /// </summary>
        public static void TrackPickupReward(string pickupType, int amount, string levelName = "")
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "level", levelName }
            };

            TrackResourceGained(DefaultCurrency, amount, "Pickup", pickupType);
            GameAnalyticsManager.Event(AnalyticsEventCategory.Economy, "pickup_reward", pickupType, amount, fields);
        }
    }
}
