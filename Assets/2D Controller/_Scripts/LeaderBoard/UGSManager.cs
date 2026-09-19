using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using TMPro;
using System.Threading.Tasks;
using LightSide;
using UnityEngine.SceneManagement;

namespace Iterations.Core
{
    public class UGSManager : MonoBehaviour
    {
        public static UGSManager Instance { get; private set; }

        public bool IsSignedIn { get; private set; }

        [Header("Tutorial Name Setup")]
        [Tooltip("This panel must be a child of the same persistent object UGSManager lives on (e.g. UI Manager), so it survives scene loads with UGSManager and this reference stays valid.")]
        [SerializeField] private GameObject tutorialNamePanel;
        [SerializeField] private TMP_InputField tutorialNameInput;
        [SerializeField] private TMP_Text tutorialErrorText;

        [SerializeField] private string tutorialSceneName = "TutorialScene";

        public const string PlayerNameKey = "PlayerName";
        public const int MaxNameLength = 8;

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
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Safety net: force-hide the tutorial panel outside the tutorial
            // scene, in case it lingers visible from a previous show.
            if (scene.name != tutorialSceneName && tutorialNamePanel != null)
            {
                tutorialNamePanel.SetActive(false);
            }
        }

        private void Start()
        {
            if (tutorialNameInput != null)
                tutorialNameInput.characterLimit = MaxNameLength;

            _ = InitializeUGS();
        }

        private async Task InitializeUGS()
        {
            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                IsSignedIn = true;

                Debug.Log("UGS initialized!");
                Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);
            }
            catch (System.Exception e)
            {
                Debug.LogError("UGS initialization failed: " + e);
            }
        }

        public bool HasSavedName() => PlayerPrefs.HasKey(PlayerNameKey);

        public string GetSavedName() => PlayerPrefs.GetString(PlayerNameKey, "");

        // Call this from GameManager right when the tutorial win panel shows.
        public void ShowTutorialNamePanelIfNeeded()
        {
            if (HasSavedName())
                return;

            if (tutorialNamePanel == null)
            {
                Debug.LogWarning("ShowTutorialNamePanelIfNeeded: tutorialNamePanel is not assigned.");
                return;
            }

            tutorialNamePanel.SetActive(true);
        }

        // Wire the tutorial panel's Confirm button to this directly in the
        // Inspector - safe, since both live on the same persistent object.
        public async void SubmitTutorialName()
        {
            if (tutorialNameInput == null)
            {
                Debug.LogWarning("SubmitTutorialName called but tutorialNameInput is null.");
                return;
            }

            bool success = await TrySetPlayerName(tutorialNameInput.text, tutorialErrorText);

            if (success && tutorialNamePanel != null)
                tutorialNamePanel.SetActive(false);
        }

        // Generic entry point any scene-local script can call (e.g. a
        // Main Menu settings panel script) without needing a direct,
        // scene-crossing reference to this object's fields.
        public async Task<bool> TrySetPlayerName(string rawName, TMP_Text errorText)
        {
            string playerName = rawName?.Trim() ?? string.Empty;

            if (!ValidateName(playerName, errorText))
                return false;

            return await UpdatePlayerName(playerName, errorText);
        }

        private bool ValidateName(string playerName, TMP_Text errorText)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                if (errorText != null) errorText.text = "Please enter a name.";
                return false;
            }

            if (playerName.Length > MaxNameLength)
            {
                if (errorText != null) errorText.text = $"Name must be {MaxNameLength} characters or less.";
                return false;
            }

            if (errorText != null) errorText.text = "";
            return true;
        }

        private async Task<bool> UpdatePlayerName(string playerName, TMP_Text errorText)
        {
            if (!IsSignedIn || AuthenticationService.Instance == null)
            {
                Debug.LogWarning("UpdatePlayerName called before UGS/auth was ready.");
                if (errorText != null) errorText.text = "Not signed in yet. Try again in a moment.";
                return false;
            }

            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

                PlayerPrefs.SetString(PlayerNameKey, playerName);
                PlayerPrefs.Save();

                if (tutorialNameInput != null)
                    tutorialNameInput.text = playerName;

                Debug.Log("Player name set to: " + playerName);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to set player name: " + e);
                if (errorText != null) errorText.text = "Failed to set name. Check the Console.";
                return false;
            }
        }

        public void RemoveSavedName()
        {
            PlayerPrefs.DeleteKey(PlayerNameKey);
            PlayerPrefs.Save();

            Debug.Log("Saved player name removed.");
        }
    }
}