using System.Collections.Generic;
using UnityEngine;

namespace KajoGames.CopyPaste.Analytics.Helpers
{
    /// <summary>
    /// High-level domain helper for instrumenting core gameplay mechanics, combat,
    /// cloning interactions, hazards, pickups, and gates.
    /// </summary>
    public static class GameplayAnalytics
    {
        #region Combat & Weapons

        /// <summary>
        /// Track when a weapon is fired (e.g. "machine_gun").
        /// </summary>
        public static void TrackWeaponFired(string weaponName = "machine_gun")
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.Combat, "weapon_fired", weaponName);
        }

        /// <summary>
        /// Track damage dealt by the player or weapon.
        /// </summary>
        public static void TrackDamageDealt(string weaponName, string targetType, float damage)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "target", targetType }
            };
            GameAnalyticsManager.Event(AnalyticsEventCategory.Combat, "damage_dealt", weaponName, damage, fields);
        }

        /// <summary>
        /// Track player death event, recording cause and retry context.
        /// </summary>
        public static void TrackPlayerDeath(string cause, string levelName, int retryCount)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "cause", cause },
                { "level", levelName },
                { "retries", retryCount }
            };
            GameAnalyticsManager.Event(AnalyticsEventCategory.Gameplay, "player_death", cause, retryCount, fields);
        }

        #endregion

        #region Cloning Mechanics

        /// <summary>
        /// Track when a new player clone is spawned in the cloning system.
        /// </summary>
        public static void TrackCloneSpawned(int cloneIndex, float recordingDuration)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "clone_index", cloneIndex },
                { "duration_sec", recordingDuration }
            };
            GameAnalyticsManager.Event(AnalyticsEventCategory.Gameplay, "clone_spawned", $"clone_{cloneIndex}", recordingDuration, fields);
        }

        /// <summary>
        /// Track when a clone replay cycle completes.
        /// </summary>
        public static void TrackCloneReplayFinished(int cloneIndex)
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.Gameplay, "clone_replay_finished", $"clone_{cloneIndex}");
        }

        #endregion

        #region Gate & Mechanics

        /// <summary>
        /// Track when a puzzle gate is opened via button or interaction.
        /// </summary>
        public static void TrackGateOpened(string gateId)
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.Gameplay, "gate_opened", gateId);
        }

        /// <summary>
        /// Track when a puzzle gate is closed.
        /// </summary>
        public static void TrackGateClosed(string gateId)
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.Gameplay, "gate_closed", gateId);
        }

        #endregion

        #region Pickups & Collectibles

        /// <summary>
        /// Track when a pickup item is collected.
        /// </summary>
        public static void TrackPickupCollected(string pickupType, int currentCount, int totalRequired = 0)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "current_count", currentCount },
                { "total_required", totalRequired }
            };
            GameAnalyticsManager.Event(AnalyticsEventCategory.Gameplay, "pickup_collected", pickupType, currentCount, fields);
        }

        /// <summary>
        /// Track when all required pickups for the active level have been gathered.
        /// </summary>
        public static void TrackAllPickupsCollected(string levelName, int totalCount)
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.Gameplay, "all_pickups_collected", levelName, totalCount);
        }

        #endregion

        #region Level Retries & Restarts

        /// <summary>
        /// Track when the player restarts a level, including cumulative retries.
        /// </summary>
        public static void TrackLevelRestart(string levelName, int retryCount)
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.Gameplay, "level_restart", levelName, retryCount);
        }

        #endregion
    }
}
