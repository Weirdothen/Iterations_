using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Iterations.Core
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Level Setup")]
        [SerializeField] private int totalLevels = 12;
        public int TotalLevels => totalLevels;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private float debugTimerInterval = 5f;

        private const string BestTimeKeyPrefix = "BestTime_";
        private const string BestRetriesKeyPrefix = "BestRetries_";
        private const string OverallScoreUnlockedKey = "OverallScoreUnlocked";



        [SerializeField] private float lastLevelTime;
        public float LastLevelTime => lastLevelTime;

        private float totalSuccessfulRunTime;
        public float TotalSuccessfulRunTime => totalSuccessfulRunTime;
        [SerializeField] private TMP_Text lastLevelTimeText;

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
            lastLevelTime = currentTime;
            // Add only the successful run of this level
            totalSuccessfulRunTime += lastLevelTime;

            if (lastLevelTimeText != null)
            {
                lastLevelTimeText.text = FormatTime(lastLevelTime);
            }

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

            

            float overallScore = GetOverallScore();

            Debug.Log(
                $"[ScoreManager] Overall Score: " +
                $"{FormatTime(overallScore)}"
            );
        }

        // Sum of retries from each completed level's best run.
        // Returns -1 if no level has been completed yet.
        public int GetOverallRetries()
        {
            int totalRetries = 0;
            int completed = 0;

            for (int i = 1; i <= totalLevels; i++)
            {
                string sceneName = "Level_" + i;

                if (GetBestTime(sceneName) < 0f)
                    continue; // not completed, skip

                totalRetries += GetBestRetries(sceneName);
                completed++;
            }

            return completed > 0 ? totalRetries : -1;
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

       

        public int GetCompletedLevelCount()
        {
            int completed = 0;

            for (int i = 1; i <= totalLevels; i++)
            {
                if (GetBestTime("Level_" + i) >= 0f)
                    completed++;
            }

            return completed;
        }

        // Sum of best times across all completed levels.
        // Returns -1 if no level has been completed yet.
        public float GetOverallScore()
        {
            float totalTime = 0f;
            int completed = 0;

            for (int i = 1; i <= totalLevels; i++)
            {
                string sceneName = "Level_" + i;
                float bestTime = GetBestTime(sceneName);

                if (bestTime < 0f)
                {
                    DebugLog($"{sceneName} not completed, skipping.");
                    continue;
                }

                totalTime += bestTime;
                completed++;
            }

            return completed > 0 ? totalTime : -1f;
        }

        public string FormatTime(float time)
        {
            if (time < 0f)
                return "--:--:---";

            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int milliseconds = Mathf.FloorToInt((time % 1f) * 1000f);

            return $"{minutes:00}:{seconds:00}:{milliseconds:000}";
        }

        private void DebugLog(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log($"[ScoreManager] {message}");
        }
        public int GetTotalRetries()
        {
            int totalRetries = 0;

            for (int i = 1; i <= totalLevels; i++)
            {
                string sceneName = "Level_" + i;

                int retries = GetBestRetries(sceneName);

                if (retries >= 0)
                {
                    totalRetries += retries;
                }
            }

            return totalRetries;
        }
    }
}