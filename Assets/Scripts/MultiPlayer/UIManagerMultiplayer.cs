using System.Collections;
using LightSide;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
// If UniText lives in a different namespace, fix the "using" line above.

namespace Iterations.UI
{
    public class UIManagerMultiplayer : MonoBehaviour
    {
        [Header("HUD Panels")]
        [SerializeField] private UniText timerText;          // The match timer (e.g. 60 seconds)
        [SerializeField] private UniText player1ScoreText;   // Host Score
        [SerializeField] private UniText player2ScoreText;   // Client Score
        [SerializeField] private UniText stateText;          // Optional: "Waiting...", "Get Ready!"

        [Header("Start Countdown (3, 2, 1)")]
        [Tooltip("A separate text object in the middle of the screen. Only visible during the countdown.")]
        [SerializeField] private UniText countdownText;
        [Tooltip("How long the pop + spin of each number lasts (seconds)")]
        [SerializeField] private float popDuration = 0.6f;
        [Tooltip("Scale each number starts from before popping in (0 = starts from nothing)")]
        [SerializeField] private float startScale = 0f;
        [Tooltip("Z rotation each number starts from and spins to 0. Use a negative value to spin the other way.")]
        [SerializeField] private float startRotation = 180f;
        [Tooltip("How far the number overshoots before settling (higher = bouncier)")]
        [SerializeField] private float popOvershoot = 1.7f;

        [Header("Menu Panels")]
        [SerializeField] private GameObject localPausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject rematchButton;

        [Header("Game Over Result")]
        [Tooltip("Shows: النتيجة: then 'your score Vs. opponent score'")]
        [SerializeField] private UniText gameOverMessageText;
        [Tooltip("Shown only to the winner")]
        [SerializeField] private GameObject winImage;
        [Tooltip("Shown only to the loser")]
        [SerializeField] private GameObject loseImage;
        [Tooltip("Text shown to both players on a draw. Leave empty to show nothing on a draw.")]
        [SerializeField] private UniText drawText;

        [Header("Host Only Buttons")]
        [Tooltip("Assign your pause menu 'Back to Lobby' button here. It will only be active for the host.")]
        [SerializeField] private GameObject backToLobbyPauseButton;

        [Header("Game Over Effects")]
        [SerializeField] private float delayBeforeGameOverUI = 1f;
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip winSound;
        [SerializeField] private AudioClip loseSound;

        private const string ResultLabel = "النتيجة:";
        private const string DrawLabel = "تعادل";

        private bool inCountdown;
        private int lastCountdownNumber = -1;
        private Coroutine popRoutine;
        private RectTransform countdownRect;

        // Latest scores, kept so the game over text can show them
        private int lastP1Score;
        private int lastP2Score;
        private bool gameOverShown;

        private void Start()
        {
            // Ensure panels are in correct starting state
            if (localPausePanel != null) localPausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            HideResultVisuals();

            if (countdownText != null)
            {
                countdownRect = countdownText.transform as RectTransform;
                countdownText.gameObject.SetActive(false);
            }

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
            // During the start countdown, the value goes to the countdown text, not the match timer
            if (inCountdown)
            {
                UpdateCountdownUI(currentTime);
                return;
            }

            if (timerText != null)
            {
                // Display timer as whole seconds
                timerText.Text = Mathf.CeilToInt(currentTime).ToString();
            }
        }

        private void UpdateCountdownUI(float currentTime)
        {
            if (countdownText == null) return;

            int number = Mathf.CeilToInt(currentTime);

            // Only animate when the number actually changes (3 -> 2 -> 1)
            if (number == lastCountdownNumber) return;
            lastCountdownNumber = number;

            if (number <= 0)
            {
                countdownText.Text = string.Empty;
                return;
            }

            countdownText.Text = number.ToString();
            PlayPop();
        }

        private void PlayPop()
        {
            if (countdownRect == null) return;

            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(PopRoutine());
        }

        // Scales the number in with a bounce while spinning it back to upright
        private IEnumerator PopRoutine()
        {
            float elapsed = 0f;

            countdownRect.localScale = Vector3.one * startScale;
            countdownRect.localRotation = Quaternion.Euler(0f, 0f, startRotation);

            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);

                float scale = Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(t));
                float rotation = Mathf.Lerp(startRotation, 0f, EaseOutCubic(t));

                countdownRect.localScale = Vector3.one * scale;
                countdownRect.localRotation = Quaternion.Euler(0f, 0f, rotation);

                yield return null;
            }

            countdownRect.localScale = Vector3.one;
            countdownRect.localRotation = Quaternion.identity;
            popRoutine = null;
        }

        private float EaseOutBack(float t)
        {
            float c1 = popOvershoot;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private void SetCountdownActive(bool active)
        {
            inCountdown = active;
            lastCountdownNumber = -1;

            if (countdownText == null) return;

            if (!active && popRoutine != null)
            {
                StopCoroutine(popRoutine);
                popRoutine = null;
            }

            countdownText.Text = string.Empty;
            countdownText.gameObject.SetActive(active);
        }


        private void UpdateScoreUI(int p1Score, int p2Score)
        {
            // Remember the scores for the game over text
            lastP1Score = p1Score;
            lastP2Score = p2Score;

            // Consider changing this logic if you want the local player's score to always be on the left
            if (player1ScoreText != null) player1ScoreText.Text = p1Score.ToString();
            if (player2ScoreText != null) player2ScoreText.Text = p2Score.ToString();

            // If the last point arrives after the game over message, keep the result text correct
            if (gameOverShown) RefreshResultText();
        }

        private void UpdateStateUI(GameManagerMultiplayer.State state)
        {
            // Show the countdown text only while the start countdown is running
            SetCountdownActive(state == GameManagerMultiplayer.State.CountdownStart);

            if (stateText != null)
            {
                switch (state)
                {
                    case GameManagerMultiplayer.State.WaitingToStart:
                        stateText.Text = "Waiting for players...";
                        break;
                    case GameManagerMultiplayer.State.CountdownStart:
                        stateText.Text = "Get Ready!";
                        break;
                    case GameManagerMultiplayer.State.GamePlaying:
                        stateText.Text = ""; // Clear text during gameplay
                        break;
                    case GameManagerMultiplayer.State.GameOver:
                        stateText.Text = "Game Over";
                        break;
                }
            }
        }

        private void ShowGameOverUI(ulong winnerId, bool isDraw, string reason)
        {
            StartCoroutine(ShowGameOverRoutine(winnerId, isDraw, reason));
        }

        private IEnumerator ShowGameOverRoutine(ulong winnerId, bool isDraw, string reason)
        {
            gameOverShown = true;

            // Make sure to hide pause menu if it was open
            if (localPausePanel != null) localPausePanel.SetActive(false);

            // Determine winner
            bool amIWinner = (!isDraw && NetworkManager.Singleton.LocalClientId == winnerId);

            // Play Sound immediately
            if (uiAudioSource != null)
            {
                if (isDraw)
                {
                    // Optional: play a draw sound, or just play nothing/lose sound
                }
                else if (amIWinner && winSound != null)
                {
                    uiAudioSource.PlayOneShot(winSound);
                }
                else if (!amIWinner && loseSound != null)
                {
                    uiAudioSource.PlayOneShot(loseSound);
                }
            }

            // Wait before showing UI
            yield return new WaitForSeconds(delayBeforeGameOverUI);

            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            // Only show the rematch button if this local client is the Party Leader
            if (rematchButton != null && GameManagerMultiplayer.Instance != null)
            {
                bool isPartyLeader = NetworkManager.Singleton.LocalClientId == GameManagerMultiplayer.Instance.PartyLeaderId;
                rematchButton.SetActive(isPartyLeader);
            }

            // Result text: the label, then "your score Vs. opponent score"
            RefreshResultText();

            // Show exactly one result: draw text, win image, or lose image
            HideResultVisuals();

            if (isDraw)
            {
                if (drawText != null)
                {
                    drawText.Text = DrawLabel;
                    drawText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (amIWinner)
                {
                    if (winImage != null) winImage.SetActive(true);
                }
                else
                {
                    if (loseImage != null) loseImage.SetActive(true);
                }
            }
        }

        private void RefreshResultText()
        {
            if (gameOverMessageText == null) return;

            // Player 1 is the host (client 0), Player 2 is the client
            bool iAmPlayer1 = NetworkManager.Singleton.LocalClientId == 0;

            int myScore = iAmPlayer1 ? lastP1Score : lastP2Score;
            int opponentScore = iAmPlayer1 ? lastP2Score : lastP1Score;

            gameOverMessageText.Text = ResultLabel + "\n" + myScore + " Vs. " + opponentScore;
        }

        private void HideResultVisuals()
        {
            if (winImage != null) winImage.SetActive(false);
            if (loseImage != null) loseImage.SetActive(false);
            if (drawText != null) drawText.gameObject.SetActive(false);
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