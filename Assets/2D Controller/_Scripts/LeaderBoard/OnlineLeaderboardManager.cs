using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using System;

namespace Iterations.Core
{
    public class OnlineLeaderboardManager : MonoBehaviour
    {
        public static OnlineLeaderboardManager Instance { get; private set; }

        [Header("Leaderboard IDs")]
        [SerializeField] private string level1LeaderboardID = "Level_1";
        [SerializeField] private string level2LeaderboardID = "Level_2";
        [SerializeField] private string overallLeaderboardID = "overall";

        private ILeaderboardsService leaderboardService;

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

        private async void Start()
        {
            try
            {
                // Wait until Unity Services are initialized
                while (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await System.Threading.Tasks.Task.Yield();
                }

                leaderboardService = UnityServices.Instance.GetLeaderboardsService();

                Debug.Log("Leaderboard service initialized!");
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "Failed to initialize Leaderboard service: " + e
                );
            }
        }

        public async void SubmitCurrentLevelScore(int score)
        {
            string leaderboardID = GetCurrentLevelLeaderboardID();

            if (string.IsNullOrEmpty(leaderboardID))
            {
                Debug.LogError("No leaderboard found for this level.");
                return;
            }

            // Wait if the leaderboard service is still initializing
            while (leaderboardService == null)
            {
                await System.Threading.Tasks.Task.Yield();
            }

            try
            {
                await leaderboardService.AddPlayerScoreAsync(
                    leaderboardID,
                    score
                );

                Debug.Log(
                    $"Submitted {score} to {leaderboardID}"
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Failed to submit score: {e}"
                );
            }
        }

        private string GetCurrentLevelLeaderboardID()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            switch (currentScene)
            {
                case "Level_1":
                    return level1LeaderboardID;

                case "Level_2":
                    return level2LeaderboardID;

                default:
                    return null;
            }
        }

        public async void SubmitOverallScore(int score)
        {
            while (leaderboardService == null)
            {
                await System.Threading.Tasks.Task.Yield();
            }

            try
            {
                await leaderboardService.AddPlayerScoreAsync(
                    overallLeaderboardID,
                    score
                );

                Debug.Log(
                    $"Submitted overall score: {score}"
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Failed to submit overall score: {e}"
                );
            }
        }
    }
}