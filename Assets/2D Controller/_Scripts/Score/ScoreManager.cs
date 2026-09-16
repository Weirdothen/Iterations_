using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Iterations.Core
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }
        public int CurrentLevelBestScore => GetCurrentLevelBestScore();
        [SerializeField] private int debugBestScore;
        [SerializeField] private int debugOverallScore;

        [Header("Score Settings")]
        [SerializeField] private int startingScore = 1000;
        [SerializeField] private float pointsLostPerSecond = 10f;

        [Header("Level Settings")]
        [SerializeField] private int totalLevels = 12;

        private const string ScoreKeyPrefix = "BestScore_";

        private float currentTime;
        private int currentScore;
        [SerializeField] TextMeshProUGUI currentScoreText;
        
        private bool timerRunning;

        public float CurrentTime => currentTime;
        public int CurrentScore => currentScore;
        public bool TimerRunning => timerRunning;

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

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenuScene")
            {
                timerRunning = false;
                return;
            }

            StartLevelTimer();
        }

        private void Update()
        {
            if (!timerRunning)
                return;

            currentTime += Time.deltaTime;
        }

        public void StartLevelTimer()
        {
            currentTime = 0f;
            currentScore = startingScore;
            timerRunning = true;
        }

        public void StopLevelTimer()
        {
            if (!timerRunning)
                return;

            timerRunning = false;

            CalculateScore();

            string currentLevel = SceneManager.GetActiveScene().name;

            SaveBestScore(currentLevel, currentScore);

            debugBestScore = GetBestScore(currentLevel);
            debugOverallScore = GetOverallScore();
        }

        private void CalculateScore()
        {
            float pointsLost = currentTime * pointsLostPerSecond;

            currentScore = Mathf.Max(
                0,
                Mathf.RoundToInt(startingScore - pointsLost)
            );
        }

        private void SaveBestScore(string levelName, int score)
        {
            string key = ScoreKeyPrefix + levelName;

            int previousBest = GetBestScore(levelName);

            if (previousBest == -1 || score > previousBest)
            {
                PlayerPrefs.SetInt(key, score);
                PlayerPrefs.Save();
            }
        }

        public int GetBestScore(string levelName)
        {
            string key = ScoreKeyPrefix + levelName;

            if (!PlayerPrefs.HasKey(key))
                return -1;

            return PlayerPrefs.GetInt(key);
        }

        public int GetCurrentLevelBestScore()
        {
            string currentLevel = SceneManager.GetActiveScene().name;

            return GetBestScore(currentLevel);
        }

        public int GetOverallScore()
        {
            int totalScore = 0;

            for (int i = 1; i <= totalLevels; i++)
            {
                string levelName = "Level" + i;

                int bestScore = GetBestScore(levelName);

                if (bestScore > 0)
                {
                    totalScore += bestScore;
                }
            }

            return totalScore;
        }
       

        public bool HaveAllLevelsBeenCompleted()
        {
            for (int i = 1; i <= totalLevels; i++)
            {
                string levelName = "Level" + i;

                if (GetBestScore(levelName) == -1)
                {
                    return false;
                }
            }

            return true;
        }
        public void ShowWinScore()
        {
            currentScoreText.text = $"Score: {currentScore}\nTime: {currentTime:F1}s";
        }
    }
}