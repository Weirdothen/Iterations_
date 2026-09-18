using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine.SceneManagement;


namespace Iterations.Core
{
    public class OnlineLeaderboardManager : MonoBehaviour
    {
        public static OnlineLeaderboardManager Instance { get; private set; }

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

        // =========================
        // SUBMIT CURRENT LEVEL
        // =========================

        public async Task SubmitCurrentLevelScore()
        {
            if (ScoreManager.Instance == null)
            {
                Debug.LogError("[LeaderboardManager] ScoreManager not found.");
                return;
            }

          

            string sceneName = SceneManager.GetActiveScene().name;
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

        // =========================
        // SUBMIT OVERALL
        // =========================

        public async void SubmitOverallScore()
        {
            if (ScoreManager.Instance == null)
            {
                Debug.LogError("[LeaderboardManager] ScoreManager not found.");
                return;
            }

            float overallTime = ScoreManager.Instance.GetOverallScore();

            if (overallTime < 0f)
            {
                Debug.Log("[LeaderboardManager] No completed levels yet, nothing to submit.");
                return;
            }

            int score = Mathf.RoundToInt(overallTime * 1000f);
            int retries = ScoreManager.Instance.GetOverallRetries();

            await SubmitScore(OverallLeaderboardId, score, retries);
        }

        // =========================
        // GET LEADERBOARD
        // =========================

        public async Task<LeaderboardScoresPage> GetCurrentLevelLeaderboard()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string leaderboardId = GetLeaderboardId(sceneName);

            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogWarning(
                    $"[LeaderboardManager] No leaderboard configured for {sceneName}"
                );

                return null;
            }

            return await GetLeaderboard(leaderboardId);
        }

        public async Task<LeaderboardScoresPage> GetLeaderboard(
            string leaderboardId)
        {
            try
            {
                Debug.Log(
                    $"[LeaderboardManager] Getting leaderboard: {leaderboardId}"
                );

                LeaderboardScoresPage response =
                    await LeaderboardsService.Instance.GetScoresAsync(
                        leaderboardId,
                        new GetScoresOptions
                        {
                            Limit = 100,
                            IncludeMetadata = true
                        }
                    );

                Debug.Log(
                    $"[LeaderboardManager] Retrieved {response.Results.Count} entries."
                );

                return response;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[LeaderboardManager] Failed to get leaderboard " +
                    $"{leaderboardId}\n{e}"
                );

                return null;
            }
        }

        // =========================
        // SUBMIT SCORE
        // =========================

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

        // =========================
        // LEADERBOARD ID
        // =========================

        private string GetLeaderboardId(string sceneName)
        {
            // Your leaderboard IDs should match these.

            if (sceneName.StartsWith("Level_"))
            {
                return sceneName;
            }

            return null;
        }

        // =========================
        // METADATA
        // =========================

        [Serializable]
        private class ScoreMetadata
        {
            public int retries;
        }
    }
}