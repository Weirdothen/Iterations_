using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Iterations.Core
{
    //TODO(Menna: Follow C# Guide)
    public class OnlineLeaderboardManager : MonoBehaviour
    {
        public event Action OnInternetRestored;

        public static OnlineLeaderboardManager Instance { get; private set; }


        private const string OverallLeaderboardId = "overall";


        private const string PendingScoresKey = "PendingLeaderboardScores";

        [System.Serializable]
        private class PendingScore
        {
            public string leaderboardId;
            public int score;
            public int retries;
        }

        [System.Serializable]
        private class PendingScoreList
        {
            public List<PendingScore> scores = new List<PendingScore>();
        }

        private bool isFlushingScores;
        private bool wasInternetAvailable;


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

        private void Start()
        {
            wasInternetAvailable =
                Application.internetReachability != NetworkReachability.NotReachable;
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

        private List<PendingScore> LoadPendingScores()
        {
            string json = PlayerPrefs.GetString(PendingScoresKey, "");

            if (string.IsNullOrEmpty(json))
                return new List<PendingScore>();

            PendingScoreList data =
                JsonUtility.FromJson<PendingScoreList>(json);

            return data?.scores ?? new List<PendingScore>();
        }

        private void SavePendingScores(List<PendingScore> scores)
        {
            PendingScoreList data = new PendingScoreList();
            data.scores = scores;

            string json = JsonUtility.ToJson(data);

            PlayerPrefs.SetString(PendingScoresKey, json);
            PlayerPrefs.Save();
        }

        // =========================
        // SUBMIT OVERALL
        // =========================

        //public async void SubmitOverallScore()
        //{
        //    if (ScoreManager.Instance == null)
        //    {
        //        Debug.LogError("[LeaderboardManager] ScoreManager not found.");
        //        return;
        //    }

        //    float overallTime = ScoreManager.Instance.GetOverallScore();

        //    if (overallTime < 0f)
        //    {
        //        Debug.Log("[LeaderboardManager] No completed levels yet, nothing to submit.");
        //        return;
        //    }

        //    int score = Mathf.RoundToInt(overallTime * 1000f);
        //    int retries = ScoreManager.Instance.GetOverallRetries();

        //    await SubmitScore(OverallLeaderboardId, score, retries);
        //}

        public async Task SubmitOverallScoreVersionTwo()
        {
            var sm = ScoreManager.Instance;
            if (sm == null)
            {
                Debug.LogError("[LeaderboardManager] ScoreManager not found.");
                return;
            }

            if (!sm.HasCompletedAllLevels())
            {
                Debug.Log($"[LeaderboardManager] Overall locked: " +
                          $"{sm.GetCompletedLevelCount()}/{sm.TotalLevels} levels completed.");
                return;
            }

            float overallTime = sm.GetOverallScore();   // sum of all 12 best times
            int retries = sm.GetOverallRetries();       // sum of retries from those best runs

            await SubmitScore(OverallLeaderboardId, Mathf.RoundToInt(overallTime * 1000f), retries);
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

        public async Task<LeaderboardScoresPage> GetOverallLeaderboard()
        {
            return await GetLeaderboard(OverallLeaderboardId);
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

        //private async Task SubmitScore(
        //    string leaderboardId,
        //    int score,
        //    int retries)
        //{
        //    try
        //    {
        //        Debug.Log(
        //            $"[LeaderboardManager] Submitting score | " +
        //            $"Leaderboard: {leaderboardId} | " +
        //            $"Score: {score}ms | " +
        //            $"Time: {score / 1000f:F3}s | " +
        //            $"Retries: {retries}"
        //        );

        //        var metadata = new ScoreMetadata
        //        {
        //            retries = retries
        //        };

        //        await LeaderboardsService.Instance.AddPlayerScoreAsync(
        //            leaderboardId,
        //            score,
        //            new AddPlayerScoreOptions
        //            {
        //                Metadata = metadata
        //            }
        //        );

        //        Debug.Log(
        //            $"[LeaderboardManager] Score submitted successfully | " +
        //            $"Leaderboard: {leaderboardId} | " +
        //            $"Score: {score}ms"
        //        );
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.LogError(
        //            $"[LeaderboardManager] Failed to submit score to " +
        //            $"{leaderboardId}\n{e}"
        //        );
        //    }
        //}

        private void Update()
        {
            bool internetAvailable =
                Application.internetReachability != NetworkReachability.NotReachable;

            if (internetAvailable && !wasInternetAvailable)
            {
                Debug.Log("[LeaderboardManager] Internet restored!");

                HandleInternetRestored();
            }

            wasInternetAvailable = internetAvailable;
        }

        private async void HandleInternetRestored()
        {
            Debug.Log("[LeaderboardManager] Internet restored!");

            if (UGSManager.Instance == null)
            {
                Debug.LogError("[LeaderboardManager] UGSManager not found.");
                return;
            }

            // Restore authentication first.
            bool signedIn = await UGSManager.Instance.EnsureSignedInAsync();

            if (!signedIn)
            {
                Debug.LogWarning("[LeaderboardManager] Could not restore authentication.");
                return;
            }

            // Upload pending scores.
            RetryPendingScores();

            while (isFlushingScores)
            {
                await Task.Yield();
            }

            // Refresh active leaderboard screens.
            Debug.Log("[LeaderboardManager] Refreshing leaderboard UI!");

            OnInternetRestored?.Invoke();
        }


        private Task SubmitScore(
     string leaderboardId,
     int score,
     int retries)
        {
            var pendingScores = LoadPendingScores();

            pendingScores.Add(new PendingScore
            {
                leaderboardId = leaderboardId,
                score = score,
                retries = retries
            });

            SavePendingScores(pendingScores);

            RetryPendingScores();

            return Task.CompletedTask;
        }

        public void RetryPendingScores()
        {
            if (isFlushingScores)
                return;

            if (Application.internetReachability ==
                NetworkReachability.NotReachable)
                return;

            if (UGSManager.Instance == null ||
                !UGSManager.Instance.IsSignedIn)
                return;

            _ = FlushPendingScores();
        }

        private async Task FlushPendingScores()
        {
            if (isFlushingScores)
                return;

            isFlushingScores = true;

            try
            {
                List<PendingScore> pendingScores = LoadPendingScores();

                while (pendingScores.Count > 0)
                {
                    if (Application.internetReachability ==
                        NetworkReachability.NotReachable)
                        break;

                    PendingScore pending = pendingScores[0];

                    bool success = await SendScoreToUGS(pending);

                    if (!success)
                    {
                        Debug.LogWarning(
                            "[LeaderboardManager] Upload failed. Keeping score for later."
                        );

                        break;
                    }

                    // Remove only after successful upload.
                    pendingScores.RemoveAt(0);

                    SavePendingScores(pendingScores);
                }

                if (pendingScores.Count == 0)
                {
                    Debug.Log("[LeaderboardManager] All pending scores uploaded!");
                }
            }
            finally
            {
                isFlushingScores = false;
            }
        }



        private async Task<bool> SendScoreToUGS(PendingScore pending)
        {
            try
            {
                var metadata = new ScoreMetadata
                {
                    retries = pending.retries
                };

                await LeaderboardsService.Instance.AddPlayerScoreAsync(
                    pending.leaderboardId,
                    pending.score,
                    new AddPlayerScoreOptions
                    {
                        Metadata = metadata
                    }
                );

                Debug.Log(
                    $"[LeaderboardManager] Uploaded pending score: " +
                    $"{pending.leaderboardId} | {pending.score}"
                );

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[LeaderboardManager] Pending score upload failed: {e}"
                );

                return false;
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

        public enum LeaderboardType
        {
            CurrentLevel,
            Overall
        }

        [SerializeField] private LeaderboardType leaderboardType;

        [Serializable]
        private class ScoreMetadata
        {
            public int retries;
        }
    }
}
