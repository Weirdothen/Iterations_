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
        [SerializeField] private VoidEventChannelSO onLevelWon;
        [SerializeField] private VoidEventChannelSO onLevelLost;
        [SerializeField] private VoidEventChannelSO onAllLevelsComplete;

        [Header("Events - Listened to by this manager")]
        [SerializeField] private VoidEventChannelSO onPauseRequested;
        [SerializeField] private VoidEventChannelSO onResumeRequested;
        [SerializeField] private VoidEventChannelSO onWinTriggered;
        [SerializeField] private VoidEventChannelSO onLoseTriggered;

        [Header("Level Flow")]
        [Tooltip("Scene to load when this level is won. Leave empty if this is the last level.")]
        private string nextLevelSceneName;
        [SerializeField] private float loseRestartDelay = 3f;

       


        public GameState CurrentState { get; private set; } = GameState.MainMenu;

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
            CurrentState = scene.name == "MainMenuScene" ? GameState.MainMenu : GameState.Playing;
            Time.timeScale = 1f;
        }

        private void HandlePauseRequested()
        {
            
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

        private void HandleWinTriggered()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Won;

            if (string.IsNullOrEmpty(nextLevelSceneName))
            {
                onAllLevelsComplete?.RaiseEvent();
                return;
            }

            onLevelWon?.RaiseEvent();
            SceneManager.LoadScene(nextLevelSceneName);
        }

        private void HandleLoseTriggered()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Lost;
            onLevelLost?.RaiseEvent();
            StartCoroutine(RestartAfterDelay());
        }

        private IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSeconds(loseRestartDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void SetNextLevel(string sceneName)
        {
            nextLevelSceneName = sceneName;
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }

}