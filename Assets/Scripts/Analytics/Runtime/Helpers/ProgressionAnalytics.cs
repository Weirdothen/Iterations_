using System.Collections.Generic;
using GameAnalyticsSDK;

namespace KajoGames.CopyPaste.Analytics.Helpers
{
    /// <summary>
    /// High-level domain helper for tracking level progression, tutorial steps,
    /// completions, fails, and high scores. Directly integrated with GameAnalytics Progression SDK.
    /// </summary>
    public static class ProgressionAnalytics
    {
        private const string DefaultProgressionTier1 = "Level";

        /// <summary>
        /// Track when a level run begins.
        /// </summary>
        public static void TrackLevelStarted(string levelName, int attemptNumber = 1)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "attempt", attemptNumber }
            };

            // GameAnalytics Native Progression Event
            GameAnalyticsManager.TrackProgression(GAProgressionStatus.Start, DefaultProgressionTier1, levelName, null, null, fields);

            // Parallel Design Event for dashboard aggregation
            GameAnalyticsManager.Event(AnalyticsEventCategory.Progression, "level_start", levelName, attemptNumber, fields);
        }

        /// <summary>
        /// Track when the player completes a level successfully, recording time, retries, and score.
        /// </summary>
        public static void TrackLevelCompleted(string levelName, float timeSeconds, int retries, int score = 0)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "time_sec", timeSeconds },
                { "retries", retries },
                { "score", score }
            };

            // GameAnalytics Native Progression Event with score
            GameAnalyticsManager.TrackProgression(GAProgressionStatus.Complete, DefaultProgressionTier1, levelName, null, score, fields);

            // Parallel Design Event with time taken as numeric value
            GameAnalyticsManager.Event(AnalyticsEventCategory.Progression, "level_complete", levelName, timeSeconds, fields);
        }

        /// <summary>
        /// Track when a level attempt fails (e.g. death by clone or trap).
        /// </summary>
        public static void TrackLevelFailed(string levelName, string failureReason, float timeSpentSeconds, int retryNumber = 1)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "reason", failureReason },
                { "time_sec", timeSpentSeconds },
                { "retry", retryNumber }
            };

            // GameAnalytics Native Progression Event
            GameAnalyticsManager.TrackProgression(GAProgressionStatus.Fail, DefaultProgressionTier1, levelName, null, null, fields);

            // Parallel Design Event
            GameAnalyticsManager.Event(AnalyticsEventCategory.Progression, "level_fail", levelName, timeSpentSeconds, fields);
        }

        /// <summary>
        /// Track the start of the tutorial.
        /// </summary>
        public static void TrackTutorialStarted()
        {
            GameAnalyticsManager.TrackProgression(GAProgressionStatus.Start, "Tutorial", "Intro");
            GameAnalyticsManager.Event(AnalyticsEventCategory.Tutorial, "tutorial_start");
        }

        /// <summary>
        /// Track progress through individual tutorial checkpoints.
        /// </summary>
        public static void TrackTutorialStep(string stepName, int stepIndex)
        {
            GameAnalyticsManager.Event(AnalyticsEventCategory.Tutorial, "tutorial_step", stepName, stepIndex);
        }

        /// <summary>
        /// Track successful completion of the tutorial.
        /// </summary>
        public static void TrackTutorialCompleted(float durationSeconds)
        {
            GameAnalyticsManager.TrackProgression(GAProgressionStatus.Complete, "Tutorial", "Complete");
            GameAnalyticsManager.Event(AnalyticsEventCategory.Tutorial, "tutorial_complete", null, durationSeconds);
        }

        /// <summary>
        /// Track when the player completes all levels in the campaign.
        /// </summary>
        public static void TrackAllLevelsCompleted(int totalRetries, float totalTimeSeconds)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "total_retries", totalRetries },
                { "total_time_sec", totalTimeSeconds }
            };
            GameAnalyticsManager.Event(AnalyticsEventCategory.Progression, "all_levels_completed", null, totalTimeSeconds, fields);
        }
    }
}
