using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Iterations.UI
{
    public class UIManagerMultiplayer : MonoBehaviour
    {
        [Header("HUD Panels")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text player1ScoreText; // Host Score
        [SerializeField] private TMP_Text player2ScoreText; // Client Score
        [SerializeField] private TMP_Text stateText; // Optional: "Waiting...", "Get Ready!"

        [Header("Menu Panels")]
        [SerializeField] private GameObject localPausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text gameOverMessageText;
        [SerializeField] private GameObject rematchButton;

        [Header("Host Only Buttons")]
        [Tooltip("Assign your pause menu 'Back to Lobby' button here. It will only be active for the host.")]
        [SerializeField] private GameObject backToLobbyPauseButton;

        private void Start()
        {
            // Ensure panels are in correct starting state
            if (localPausePanel != null) localPausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // Subscribe to GameManagerMultiplayer events
            if (GameManagerMultiplayer.Instance != null)
            {
                GameManagerMultiplayer.Instance.OnTimerUpdated += UpdateTimerUI;
                GameManagerMultiplayer.Instance.OnScoreUpdated += UpdateScoreUI;
                GameManagerMultiplayer.Instance.OnStateChanged += UpdateStateUI;
                GameManagerMultiplayer.Instance.OnGameOverEvent += ShowGameOverUI;
            }
            else
            {
                Debug.LogWarning("UIManagerMultiplayer: GameManagerMultiplayer.Instance is null. Events not bound. Make sure GameManagerMultiplayer exists in the scene.");
            }
        }

        private void OnDestroy()
        {
            if (GameManagerMultiplayer.Instance != null)
            {
                GameManagerMultiplayer.Instance.OnTimerUpdated -= UpdateTimerUI;
                GameManagerMultiplayer.Instance.OnScoreUpdated -= UpdateScoreUI;
                GameManagerMultiplayer.Instance.OnStateChanged -= UpdateStateUI;
                GameManagerMultiplayer.Instance.OnGameOverEvent -= ShowGameOverUI;
            }
        }

        private void Update()
        {
            // Toggle local pause menu overlay
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Don't allow pausing if game is over
                if (gameOverPanel != null && gameOverPanel.activeSelf) return;

                if (localPausePanel != null)
                {
                    bool willShow = !localPausePanel.activeSelf;
                    localPausePanel.SetActive(willShow);

                    // When opening the pause menu, check if we are the host to show the Back to Lobby button
                    if (willShow && backToLobbyPauseButton != null && NetworkManager.Singleton != null)
                    {
                        backToLobbyPauseButton.SetActive(NetworkManager.Singleton.IsHost);
                    }
                }
            }
        }

        private void UpdateTimerUI(float currentTime)
        {
            if (timerText != null)
            {
                // Display timer as whole seconds
                timerText.text = Mathf.CeilToInt(currentTime).ToString();
            }
        }

        private void UpdateScoreUI(int p1Score, int p2Score)
        {
            // Consider changing this logic if you want the local player's score to always be on the left
            if (player1ScoreText != null) player1ScoreText.text = p1Score.ToString();
            if (player2ScoreText != null) player2ScoreText.text = p2Score.ToString();
        }

        private void UpdateStateUI(GameManagerMultiplayer.State state)
        {
            if (stateText != null)
            {
                switch (state)
                {
                    case GameManagerMultiplayer.State.WaitingToStart:
                        stateText.text = "Waiting for players...";
                        break;
                    case GameManagerMultiplayer.State.CountdownStart:
                        stateText.text = "Get Ready!";
                        break;
                    case GameManagerMultiplayer.State.GamePlaying:
                        stateText.text = ""; // Clear text during gameplay
                        break;
                    case GameManagerMultiplayer.State.GameOver:
                        stateText.text = "Game Over";
                        break;
                }
            }
        }

        private void ShowGameOverUI(ulong winnerId, bool isDraw, string reason)
        {
            // Make sure to hide pause menu if it was open
            if (localPausePanel != null) localPausePanel.SetActive(false);

            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            
            // Only show the rematch button if this local client is the Party Leader
            if (rematchButton != null && GameManagerMultiplayer.Instance != null)
            {
                bool isPartyLeader = NetworkManager.Singleton.LocalClientId == GameManagerMultiplayer.Instance.PartyLeaderId;
                rematchButton.SetActive(isPartyLeader);
            }

            if (gameOverMessageText != null)
            {
                if (isDraw)
                {
                    gameOverMessageText.text = "It's a Draw!\n" + reason;
                }
                else
                {
                    // Check if the local player is the winner
                    bool amIWinner = (NetworkManager.Singleton.LocalClientId == winnerId);
                    
                    if (amIWinner)
                    {
                        gameOverMessageText.text = "You Win!\n" + reason;
                    }
                    else
                    {
                        gameOverMessageText.text = "You Lose!\n" + reason;
                    }
                }
            }
        }

        // --- UI Button Callbacks ---

        public void OnResumePressed()
        {
            if (localPausePanel != null) localPausePanel.SetActive(false);
        }

        public void OnLeaveMatchPressed()
        {
            // Flag this as intentional so the disconnect popup doesn't appear
            ConnectionFailedData.IntentionalDisconnect = true;

            // Shutdown the network session. This safely disconnects us.
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // Return to the main menu
            SceneManager.LoadScene("MainMenuScene"); 
        }

        public void OnBackToLobbyPressed()
        {
            // Hook this to the Back to Lobby button in your pause menu
            if (GameManagerMultiplayer.Instance != null)
            {
                GameManagerMultiplayer.Instance.ReturnToLobby();
            }
        }

        public void OnRematchPressed()
        {
            if (GameManagerMultiplayer.Instance != null)
            {
                GameManagerMultiplayer.Instance.ReturnToLobby();
            }
        }
    }
}
