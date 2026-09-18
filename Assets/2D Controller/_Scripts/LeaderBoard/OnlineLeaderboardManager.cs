using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Leaderboards;

namespace Iterations.Core
{
    public class OnlineLeaderboardManager : MonoBehaviour
    {
        public static OnlineLeaderboardManager Instance { get; private set; }

        private const string Level1LeaderboardId = "Level_1";
        private const string Level2LeaderboardId = "Level_2";
        private const string OverallLeaderboardId = "overall";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async void SubmitCurrentLevelScore()
        {
            if (ScoreManager.Instance == null)
            {
                Debug.LogError("[LeaderboardManager] ScoreManager not found.");
                return;
            }

            if (!ScoreManager.Instance.LastRunWasNewBest)
            {
                Debug.Log(
                    "[LeaderboardManager] Current run was not a new best. " +
                    "No leaderboard submission needed."
                );

                return;
            }

            string sceneName = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().name;

            string leaderboardId = GetLeaderboardId(sceneName);

            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogWarning(
                    $"[LeaderboardManager] No leaderboard configured for scene: {sceneName}"
                );

                return;
            }

            float bestTime = ScoreManager.Instance.GetBestTime(sceneName);
            int retries = ScoreManager.Instance.GetBestRetries(sceneName);

            int score = Mathf.RoundToInt(bestTime * 1000f);

            await SubmitScore(
                leaderboardId,
                score,
                retries
            );
        }

        public async void SubmitOverallScore()
        {
            if (ScoreManager.Instance == null)
            {
                Debug.LogError("[LeaderboardManager] ScoreManager not found.");
                return;
            }

            if (!ScoreManager.Instance.OverallScoreUnlocked)
            {
                Debug.Log(
                    "[LeaderboardManager] Overall leaderboard is still locked."
                );

                return;
            }

            float overallTime = ScoreManager.Instance.GetOverallScore();

            if (overallTime < 0f)
            {
                Debug.LogWarning(
                    "[LeaderboardManager] Could not calculate overall score."
                );

                return;
            }

            int score = Mathf.RoundToInt(overallTime * 1000f);

            await SubmitScore(
                OverallLeaderboardId,
                score,
                0
            );
        }

        private async Task SubmitScore(
            string leaderboardId,
            int score,
            int retries)
        {
            try
            {
                Debug.Log(
                    $"[LeaderboardManager] Submitting score | " +
                    $"Leaderboard: {leaderboardId} | " +
                    $"Score: {score}ms | " +
                    $"Time: {score / 1000f:F3}s | " +
                    $"Retries: {retries}"
                );

                var metadata = new ScoreMetadata
                {
                    retries = retries
                };

                var response =
                    await LeaderboardsService.Instance.AddPlayerScoreAsync(
                        leaderboardId,
                        score,
                        new AddPlayerScoreOptions
                        {
                            Metadata = metadata
                        }
                    );

                Debug.Log(
                    $"[LeaderboardManager] Score submitted successfully | " +
                    $"Leaderboard: {leaderboardId} | " +
                    $"Score: {score}ms"
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[LeaderboardManager] Failed to submit score to " +
                    $"{leaderboardId}\n{e}"
                );
            }
        }

        private string GetLeaderboardId(string sceneName)
        {
            switch (sceneName)
            {
                case "Level_1":
                    return Level1LeaderboardId;

                case "Level_2":
                    return Level2LeaderboardId;

                default:
                    return null;
            }
        }

        [Serializable]
        private class ScoreMetadata
        {
            public int retries;
        }
    }
}