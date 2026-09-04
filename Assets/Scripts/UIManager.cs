using Iterations.Events;
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
        [SerializeField] private GameObject losePanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Events - Listened to by this manager")]
        [SerializeField] private VoidEventChannelSO onLevelWon;
        [SerializeField] private VoidEventChannelSO onLevelLost;
        [SerializeField] private VoidEventChannelSO onAllLevelsComplete;

        [Header("Events - Raised by this manager")]
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
            if (onLevelWon != null) onLevelWon.OnEventRaised += HandleLevelWon;
            if (onLevelLost != null) onLevelLost.OnEventRaised += HandleLevelLost;
            if (onAllLevelsComplete != null) onAllLevelsComplete.OnEventRaised += HandleAllLevelsComplete;
            if (onPauseRequested != null) onPauseRequested.OnEventRaised += HandlePauseRequested;
            if (onResumeRequested != null) onResumeRequested.OnEventRaised += HandleResumeRequested;

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            if (onLevelWon != null) onLevelWon.OnEventRaised -= HandleLevelWon;
            if (onLevelLost != null) onLevelLost.OnEventRaised -= HandleLevelLost;
            if (onAllLevelsComplete != null) onAllLevelsComplete.OnEventRaised -= HandleAllLevelsComplete;
            if (onPauseRequested != null) onPauseRequested.OnEventRaised -= HandlePauseRequested;
            if (onResumeRequested != null) onResumeRequested.OnEventRaised -= HandleResumeRequested;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("ESC pressed");
                if (pausePanel != null && pausePanel.activeSelf)
                    onResumeRequested?.RaiseEvent();
                else
                    onPauseRequested?.RaiseEvent();
            }
        }

        public void OnContinuePressed()
        {
            onResumeRequested?.RaiseEvent();
        }

        private void HandleLevelWon()
        {
            Debug.Log("You Won");
            if (winPanel != null)
                winPanel.SetActive(true);
        }

        private void HandleLevelLost()
        {
            Debug.Log("You Lose");
            if (losePanel != null)
                losePanel.SetActive(true);
        }

        private void HandleAllLevelsComplete()
        {
            Debug.Log("You Won - No More Levels");
            if (winPanel != null)
                winPanel.SetActive(true);
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

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }
    }

}