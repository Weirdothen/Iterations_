using UnityEngine;
using UnityEngine.SceneManagement;

namespace Iterations.Core
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Level Setup")]
        [SerializeField] private int totalLevels = 12;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private float debugTimerInterval = 5f;

        private const string BestTimeKeyPrefix = "BestTime_";
        private const string BestRetriesKeyPrefix = "BestRetries_";
        private const string OverallScoreUnlockedKey = "OverallScoreUnlocked";

        private float currentTime;
        private bool isTiming;
        private float nextDebugTime;

        public float CurrentTime => currentTime;

        public bool LastRunWasNewBest { get; private set; }

        public float BestTimeForCurrentLevel
        {
            get
            {
                string sceneName = SceneManager.GetActiveScene().name;
                return GetBestTime(sceneName);
            }
        }

        public int BestRetriesForCurrentLevel
        {
            get
            {
                string sceneName = SceneManager.GetActiveScene().name;
                return GetBestRetries(sceneName);
            }
        }

        public bool OverallScoreUnlocked
        {
            get
            {
                return PlayerPrefs.GetInt(OverallScoreUnlockedKey, 0) == 1;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            DebugLog("ScoreManager initialized.");
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (!isTiming)
                return;

            currentTime += Time.deltaTime;

            if (enableDebugLogs && currentTime >= nextDebugTime)
            {
                Debug.Log(
                    $"[ScoreManager] Timer running | " +
                    $"Level: {SceneManager.GetActiveScene().name} | " +
                    $"Time: {currentTime:F2}s"
                );

                nextDebugTime = currentTime + debugTimerInterval;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DebugLog($"Scene loaded: {scene.name}");

            if (scene.name == "MainMenuScene")
            {
                isTiming = false;
                currentTime = 0f;
                LastRunWasNewBest = false;

                DebugLog("Main Menu detected. Timer stopped.");
                return;
            }

            StartLevelTimer();
        }

        public void StartLevelTimer()
        {
            currentTime = 0f;
            isTiming = true;
            LastRunWasNewBest = false;
            nextDebugTime = debugTimerInterval;

            string sceneName = SceneManager.GetActiveScene().name;

            Debug.Log(
                $"[ScoreManager] TIMER STARTED | " +
                $"Level: {sceneName}"
            );

            float existingBest = GetBestTime(sceneName);

            if (existingBest < 0f)
            {
                DebugLog("No previous best time for this level.");
            }
            else
            {
                int existingRetries = GetBestRetries(sceneName);

                Debug.Log(
                    $"[ScoreManager] Existing best | " +
                    $"Time: {existingBest:F2}s | " +
                    $"Retries: {existingRetries}"
                );
            }
        }

        public void StopLevelTimer()
        {
            if (!isTiming)
            {
                DebugLog("StopLevelTimer called, but timer was already stopped.");
                return;
            }

            isTiming = false;
            LastRunWasNewBest = false;

            string sceneName = SceneManager.GetActiveScene().name;

            int retries = GameManager.Instance != null
                ? GameManager.Instance.CurrentLevelRetries
                : 0;

            Debug.Log(
                $"[ScoreManager] TIMER STOPPED | " +
                $"Level: {sceneName} | " +
                $"Final Time: {currentTime:F2}s | " +
                $"Retries: {retries}"
            );

            SaveBestTimeIfBetter(sceneName, currentTime, retries);

            CheckOverallScoreUnlock();

            float overallScore = GetOverallScore();

            Debug.Log(
                $"[ScoreManager] Overall Score: " +
                $"{FormatTime(overallScore)}"
            );
        }

        private void SaveBestTimeIfBetter(
            string sceneName,
            float time,
            int retries)
        {
            string timeKey = BestTimeKeyPrefix + sceneName;
            string retriesKey = BestRetriesKeyPrefix + sceneName;

            if (!PlayerPrefs.HasKey(timeKey))
            {
                PlayerPrefs.SetFloat(timeKey, time);
                PlayerPrefs.SetInt(retriesKey, retries);
                PlayerPrefs.Save();

                LastRunWasNewBest = true;

                Debug.Log(
                    $"[ScoreManager] NEW BEST TIME | " +
                    $"{sceneName}: {time:F2}s | " +
                    $"Retries: {retries} | " +
                    $"First completion!"
                );

                return;
            }

            float oldBestTime = PlayerPrefs.GetFloat(timeKey);
            int oldBestRetries = PlayerPrefs.GetInt(retriesKey, 0);

            if (time < oldBestTime)
            {
                PlayerPrefs.SetFloat(timeKey, time);
                PlayerPrefs.SetInt(retriesKey, retries);
                PlayerPrefs.Save();

                LastRunWasNewBest = true;

                Debug.Log(
                    $"[ScoreManager] NEW BEST TIME | " +
                    $"{sceneName}: {time:F2}s | " +
                    $"Previous: {oldBestTime:F2}s | " +
                    $"Improvement: {oldBestTime - time:F2}s | " +
                    $"Retries: {retries}"
                );
            }
            else
            {
                Debug.Log(
                    $"[ScoreManager] Time was NOT a new best | " +
                    $"{sceneName}: {time:F2}s | " +
                    $"Best remains: {oldBestTime:F2}s | " +
                    $"Best retries: {oldBestRetries}"
                );
            }
        }

        public float GetBestTime(string sceneName)
        {
            string key = BestTimeKeyPrefix + sceneName;

            if (!PlayerPrefs.HasKey(key))
                return -1f;

            return PlayerPrefs.GetFloat(key);
        }

        public int GetBestRetries(string sceneName)
        {
            string key = BestRetriesKeyPrefix + sceneName;

            if (!PlayerPrefs.HasKey(key))
                return -1;

            return PlayerPrefs.GetInt(key);
        }

        private void CheckOverallScoreUnlock()
        {
            int completedLevels = 0;

            DebugLog("Checking Overall Score unlock...");

            for (int i = 1; i <= totalLevels; i++)
            {
                string sceneName = "Level_" + i;
                float bestTime = GetBestTime(sceneName);

                if (bestTime >= 0f)
                {
                    completedLevels++;

                    int retries = GetBestRetries(sceneName);

                    DebugLog(
                        $"Level {i} completed | " +
                        $"Best: {bestTime:F2}s | " +
                        $"Retries: {retries}"
                    );
                }
                else
                {
                    DebugLog(
                        $"Level {i} has not been completed yet."
                    );
                }
            }

            Debug.Log(
                $"[ScoreManager] Levels completed: " +
                $"{completedLevels}/{totalLevels}"
            );

            if (completedLevels >= totalLevels)
            {
                PlayerPrefs.SetInt(OverallScoreUnlockedKey, 1);
                PlayerPrefs.Save();

                Debug.Log(
                    "[ScoreManager] ★ OVERALL SCORE UNLOCKED ★"
                );

                Debug.Log(
                    $"[ScoreManager] Overall Score: " +
                    $"{FormatTime(GetOverallScore())}"
                );
            }
            else
            {
                DebugLog("Overall Score is still locked.");
            }
        }

        public float GetOverallScore()
        {
            if (!OverallScoreUnlocked)
                return -1f;

            float totalTime = 0f;

            for (int i = 1; i <= totalLevels; i++)
            {
                string sceneName = "Level_" + i;
                float bestTime = GetBestTime(sceneName);

                if (bestTime < 0f)
                {
                    DebugLog(
                        $"Cannot calculate Overall Score. " +
                        $"{sceneName} has no best time."
                    );

                    return -1f;
                }

                totalTime += bestTime;
            }

            return totalTime;
        }

        private string FormatTime(float time)
        {
            if (time < 0f)
                return "LOCKED";

            int totalSeconds = Mathf.FloorToInt(time);

            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }

        private void DebugLog(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log($"[ScoreManager] {message}");
        }
    }
}