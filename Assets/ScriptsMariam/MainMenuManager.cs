using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
namespace Iterations.UI
{
    /// <summary>
    /// Attach to your Main Menu Canvas.
    /// Handles Host and Join flows via MultiplayerRelayManager.
    /// Wire the UI elements in the Inspector.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject soloPanel;
        [SerializeField] private GameObject multiplayerPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject controlsPanel;
        [Header("Multiplayer UI")]
        [Tooltip("The input field where the player types a Join Code.")]
        [SerializeField] private TMP_InputField joinCodeInput;
        [Tooltip("A status label to show 'Connecting...', errors, etc.")]
        [SerializeField] private TMP_Text multiplayerStatusText;
        [Tooltip("Disable Host/Join buttons while a connection is in progress.")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        private GameObject[] _allPanels;
        private void Awake()
        {
            _allPanels = new GameObject[]
            {
                mainPanel,
                creditsPanel,
                soloPanel,
                multiplayerPanel,
                settingsPanel,
                controlsPanel
            };
            ShowPanel(mainPanel);
        }
        // ─── Panel Navigation ──────────────────────────────────────────────────
        public void OnCreditsPressed() => ShowPanel(creditsPanel);
        public void OnSoloPressed() => ShowPanel(soloPanel);
        public void OnMultiplayerPressed() => ShowPanel(multiplayerPanel);
        public void OnSettingsPressed() => ShowPanel(settingsPanel);
        public void OnControlsPressed() => ShowPanel(controlsPanel);
        public void OnBackPressed() => ShowPanel(mainPanel);
        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        public void LoadSceneByName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("No scene name set on this button.");
                return;
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        // ─── Multiplayer Flows ─────────────────────────────────────────────────
        /// <summary>Called by the "Host" button.</summary>
        public async void StartAsHost()
        {
            if (MultiplayerRelayManager.Instance == null)
            {
                Debug.LogError("[MainMenu] MultiplayerRelayManager not found in scene!");
                return;
            }
            SetMultiplayerUIBusy(true, "Creating session...");
            bool success = await MultiplayerRelayManager.Instance.CreateRelayAndHost();
            if (!success)
            {
                SetMultiplayerUIBusy(false, "Failed to create session. Try again.");
            }
            // On success the scene transitions away, so we don't need to reset UI.
        }
        /// <summary>Called by the "Join" button.</summary>
        public async void StartAsClient()
        {
            if (MultiplayerRelayManager.Instance == null)
            {
                Debug.LogError("[MainMenu] MultiplayerRelayManager not found in scene!");
                return;
            }
            string code = joinCodeInput != null ? joinCodeInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(code))
            {
                SetMultiplayerUIBusy(false, "Please enter a Join Code.");
                return;
            }
            SetMultiplayerUIBusy(true, "Joining session...");
            bool success = await MultiplayerRelayManager.Instance.JoinRelayAndConnect(code);
            if (!success)
            {
                // The RelayManager also sets ConnectionFailedData so the popup fires
                SetMultiplayerUIBusy(false, "Could not join. Check the code and try again.");
            }
        }
        // ─── Helpers ───────────────────────────────────────────────────────────
        private void SetMultiplayerUIBusy(bool busy, string statusMessage)
        {
            if (multiplayerStatusText != null) multiplayerStatusText.text = statusMessage;
            if (hostButton != null) hostButton.interactable = !busy;
            if (joinButton != null) joinButton.interactable = !busy;
        }
        private void ShowPanel(GameObject panelToShow)
        {
            foreach (var panel in _allPanels)
            {
                if (panel == null) continue;
                panel.SetActive(panel == panelToShow);
            }
        }
    }
}
