using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Iterations.Events;

namespace Iterations.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Won,
        Lost
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Events - Raised by this manager")]
        [SerializeField] private VoidEventChannelSO onAllLevelsComplete;
        [SerializeField] private IntEventChannelSO onLevelWonWithRetries;

        [Header("Events - Listened to by this manager")]
        [SerializeField] private VoidEventChannelSO onPauseRequested;
        [SerializeField] private VoidEventChannelSO onResumeRequested;
        [SerializeField] private VoidEventChannelSO onWinTriggered;
        [SerializeField] private VoidEventChannelSO onLoseTriggered;

        [Header("Level Flow")]
        private string nextLevelSceneName;
        [SerializeField] private float loseRestartDelay = 4f;

        [SerializeField] private string mainMenuSceneName = "MainMenuScene";

        [Header("Tutorial")]
        [SerializeField] private string tutorialSceneName = "TutorialScene";
        [SerializeField] private GameObject tutorialWinPanel;

        [Header("Retries")]
        private const string RetriesKeyPrefix = "Retries_";

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        public int CurrentLevelRetries { get; private set; }
        public int BestRetriesForCurrentLevel { get; private set; }

        private Coroutine _restartRoutine;
        private bool _levelAdvancePending;

        private bool _isSameLevelReload;

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
            if (onPauseRequested != null) onPauseRequested.OnEventRaised += HandlePauseRequested;
            if (onResumeRequested != null) onResumeRequested.OnEventRaised += HandleResumeRequested;
            if (onWinTriggered != null) onWinTriggered.OnEventRaised += HandleWinTriggered;
            if (onLoseTriggered != null) onLoseTriggered.OnEventRaised += HandleLoseTriggered;

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            if (onPauseRequested != null) onPauseRequested.OnEventRaised -= HandlePauseRequested;
            if (onResumeRequested != null) onResumeRequested.OnEventRaised -= HandleResumeRequested;
            if (onWinTriggered != null) onWinTriggered.OnEventRaised -= HandleWinTriggered;
            if (onLoseTriggered != null) onLoseTriggered.OnEventRaised -= HandleLoseTriggered;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentState = scene.name == mainMenuSceneName ? GameState.MainMenu : GameState.Playing;

            Time.timeScale = 1f;
            AudioListener.pause = false;

            _restartRoutine = null;
            _levelAdvancePending = false;

            if (_isSameLevelReload)
            {

            }
            else
            {
                CurrentLevelRetries = 0;
            }

            _isSameLevelReload = false;

            BestRetriesForCurrentLevel = GetSavedBestRetries(scene.name);
        }

        private void HandlePauseRequested()
        {
            // Pausing (e.g. via Esc) is disabled entirely while in the
            // tutorial scene.
            if (SceneManager.GetActiveScene().name == tutorialSceneName)
                return;

            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
        }

        private void HandleResumeRequested()
        {
            if (CurrentState != GameState.Paused) return;

            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
        }

        private async void HandleWinTriggered()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Won;

            ScoreManager.Instance?.StopLevelTimer();

            string currentSceneName = SceneManager.GetActiveScene().name;
            bool isTutorial = currentSceneName == tutorialSceneName;

            Time.timeScale = 0f;
            AudioListener.pause = true;

            if (isTutorial)
            {
                if (tutorialWinPanel != null)
                {
                    tutorialWinPanel.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("HandleWinTriggered: tutorialWinPanel is not assigned.");
                }

                UGSManager.Instance?.ShowTutorialNamePanelIfNeeded();
            }
            else
            {
                if (OnlineLeaderboardManager.Instance != null)
                {
                    await OnlineLeaderboardManager.Instance.SubmitCurrentLevelScore();
                    OnlineLeaderboardManager.Instance.SubmitOverallScore();
                }

                LeaderboardUI.Instance?.ShowLeaderboard();
            }

            SaveRetriesIfBest(currentSceneName, CurrentLevelRetries);

            if (!string.IsNullOrEmpty(nextLevelSceneName))
            {
                PlayerPrefs.SetInt(nextLevelSceneName, 1);
                PlayerPrefs.Save();
            }

            _levelAdvancePending = true;
            onLevelWonWithRetries?.RaiseEvent(CurrentLevelRetries);
        }

        public void AdvanceAfterWin()
        {
            if (!_levelAdvancePending) return;
            _levelAdvancePending = false;

            if (string.IsNullOrEmpty(nextLevelSceneName))
            {
                onAllLevelsComplete?.RaiseEvent();
                return;
            }

            SceneTransitioner.LoadScene(nextLevelSceneName);
        }

        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleLoseTriggered()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Lost;

            if (_restartRoutine != null) return;

            _restartRoutine = StartCoroutine(RestartAfterDelay());
        }

        private IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSeconds(loseRestartDelay);
            RestartLevel();
        }

        private int GetSavedBestRetries(string sceneName)
        {
            string key = RetriesKeyPrefix + sceneName;
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : -1;
        }

        private void SaveRetriesIfBest(string sceneName, int retries)
        {
            string key = RetriesKeyPrefix + sceneName;

            if (!PlayerPrefs.HasKey(key) || retries < PlayerPrefs.GetInt(key))
            {
                PlayerPrefs.SetInt(key, retries);
                PlayerPrefs.Save();
                BestRetriesForCurrentLevel = retries;
            }
        }

        public void SetNextLevel(string sceneName)
        {
            nextLevelSceneName = sceneName;
        }

        public void RestartLevel()
        {
            CurrentLevelRetries++;
            _isSameLevelReload = true;
            SceneTransitioner.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void RestartLevelFresh()
        {
            CurrentLevelRetries = 0;
            _isSameLevelReload = true;
            SceneTransitioner.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMainMenu()
        {
            SceneTransitioner.LoadScene(mainMenuSceneName);
        }

        public void LoadLevelFromMenu(string sceneName)
        {
            SceneTransitioner.LoadScene(sceneName);
        }
    }
}