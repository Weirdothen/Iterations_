using Iterations.Core;
using Iterations.Events;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Iterations.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject allLevelsWinPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject controlsPanel;

        [Header("Win Panel - Retries Display")]
        [SerializeField] private TMP_Text retriesText;
        [SerializeField] private string retriesLabelFormat = "{0}";

        [Header("All Levels Win - Total Retries")]
        [SerializeField] private TMP_Text totalRetriesText;

        [SerializeField] private TMP_Text totalSuccessfulTimeText;

        [Header("Tutorial")]
        [SerializeField] private string tutorialSceneName = "TutorialScene";

        [Header("Events - Listened to by this manager")]
        [SerializeField] private IntEventChannelSO onLevelWonWithRetries;
        [SerializeField] private VoidEventChannelSO onAllLevelsComplete;
        [SerializeField] private VoidEventChannelSO onPauseRequested;
        [SerializeField] private VoidEventChannelSO onResumeRequested;

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
            if (onLevelWonWithRetries != null) onLevelWonWithRetries.OnEventRaised += HandleLevelWonWithRetries;
            if (onAllLevelsComplete != null) onAllLevelsComplete.OnEventRaised += HandleAllLevelsComplete;
            if (onPauseRequested != null) onPauseRequested.OnEventRaised += HandlePauseRequested;
            if (onResumeRequested != null) onResumeRequested.OnEventRaised += HandleResumeRequested;

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            if (onLevelWonWithRetries != null) onLevelWonWithRetries.OnEventRaised -= HandleLevelWonWithRetries;
            if (onAllLevelsComplete != null) onAllLevelsComplete.OnEventRaised -= HandleAllLevelsComplete;
            if (onPauseRequested != null) onPauseRequested.OnEventRaised -= HandlePauseRequested;
            if (onResumeRequested != null) onResumeRequested.OnEventRaised -= HandleResumeRequested;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            // Esc is fully disabled in the tutorial scene - no pause panel,
            // no state change, nothing.
            if (SceneManager.GetActiveScene().name == tutorialSceneName) return;

            if (GameManager.Instance == null) return;

            var state = GameManager.Instance.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused) return;

            if (pausePanel != null && pausePanel.activeSelf)
                onResumeRequested?.RaiseEvent();
            else
                onPauseRequested?.RaiseEvent();
        }

        public void OnContinuePressed()
        {
            onResumeRequested?.RaiseEvent();
        }


        private void HandleLevelWonWithRetries(int retries)
        {
            if (retriesText != null)
                retriesText.text = string.Format(retriesLabelFormat, retries);

            if (winPanel != null)
                winPanel.SetActive(true);
        }

        private void HandleAllLevelsComplete()
        {
            if (totalRetriesText != null && ScoreManager.Instance != null)
            {
                int totalRetries = ScoreManager.Instance.GetTotalRetries();

                totalRetriesText.text = totalRetries.ToString();
            }
             if (totalSuccessfulTimeText != null && ScoreManager.Instance != null)
            {
                totalSuccessfulTimeText.text =
                    ScoreManager.Instance.FormatTime(
                        ScoreManager.Instance.TotalSuccessfulRunTime
                    );
            }

            if (allLevelsWinPanel != null)
                allLevelsWinPanel.SetActive(true);
        }
       

        private void HandlePauseRequested()
        {
            if (pausePanel != null)
                pausePanel.SetActive(true);
        }

        private void HandleResumeRequested()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        public void ShowCreditsPanel()
        {
            if (creditsPanel != null)
                creditsPanel.SetActive(true);
        }

        public void HideCreditsPanel()
        {
            if (creditsPanel != null)
                creditsPanel.SetActive(false);
        }

        public void ShowControlsPanel()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(true);
        }

        public void HideControlsPanel()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (allLevelsWinPanel != null) allLevelsWinPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
        }
    }
}