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

        [Header("Startup Name Setup")]
        [SerializeField] private GameObject startupNamePanel;
       
        [SerializeField] private TMP_InputField startupNameInput;
        [SerializeField] private TMP_Text startupErrorText;

        [Header("Settings Name Setup")]
        [SerializeField] private GameObject settingsNamePanel;
        [SerializeField] private TMP_InputField settingsNameInput;
        [SerializeField] private TMP_Text settingsErrorText;

        private const string PlayerNameKey = "PlayerName";
        private const int MaxNameLength = 8;

        private void ApplyNameLimit()
        {
            if (startupNameInput != null)
                startupNameInput.characterLimit = MaxNameLength;

            if (settingsNameInput != null)
                settingsNameInput.characterLimit = MaxNameLength;
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
            if (scene.name == "MainMenuScene")
            {
                FindMainMenuNameUI();
            }
        }

        private void FindMainMenuNameUI()
        {
            GameObject panel = GameObject.Find("StartingNamePanel (1)");

            if (panel == null)
                return;

            startupNamePanel = panel;

            startupNameInput =
                panel.GetComponentInChildren<TMP_InputField>(true);

            startupErrorText =
                panel.GetComponentInChildren<TMP_Text>(true);

            ApplyNameLimit(); // <-- add this
            if (PlayerPrefs.HasKey(PlayerNameKey))
            {
                string savedName = PlayerPrefs.GetString(PlayerNameKey);

                startupNameInput.text = savedName;
                startupNamePanel.SetActive(false);

                Debug.Log("Returning to Main Menu - name already exists.");
            }
            else
            {
                startupNamePanel.SetActive(true);
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
        }

        private void Start()
        {
            ApplyNameLimit();
            InitializeUGS();
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

                LoadPlayerName();
            }
            catch (System.Exception e)
            {
                Debug.LogError("UGS initialization failed: " + e);
            }
        }

        private void LoadPlayerName()
        {
            if (PlayerPrefs.HasKey(PlayerNameKey))
            {
                string savedName = PlayerPrefs.GetString(PlayerNameKey);

                // Put the saved name into both input fields
                startupNameInput.text = savedName;
                settingsNameInput.text = savedName;

                // A name already exists, so don't show the startup panel
                startupNamePanel.SetActive(false);

                Debug.Log("Loaded player name: " + savedName);
            }
            else
            {
                // First time playing
                startupNamePanel.SetActive(true);

                
            }
        }

        public async void SubmitStartupName()
        {
            string playerName = startupNameInput.text.Trim();

            if (!ValidateName(playerName, startupErrorText))
                return;

            await UpdatePlayerName(playerName, startupErrorText);

            if (IsSignedIn)
            {
                startupNamePanel.SetActive(false);
            }
        }

        public async void SubmitSettingsName()
        {
            string playerName = settingsNameInput.text.Trim();

            if (!ValidateName(playerName, settingsErrorText))
                return;

            await UpdatePlayerName(playerName, settingsErrorText);

           
        }

        private bool ValidateName(string playerName, TMP_Text errorText)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                errorText.text = "Please enter a name.";
                return false;
            }

            if (playerName.Length > MaxNameLength)
            {
                errorText.text = $"Name must be {MaxNameLength} characters or less.";
                return false;
            }


            errorText.text = "";
            return true;
        }

        private async Task UpdatePlayerName(string playerName, TMP_Text errorText)
        {
            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

                // Save name locally
                PlayerPrefs.SetString(PlayerNameKey, playerName);
                PlayerPrefs.Save();

                // Keep both input fields synchronized
                startupNameInput.text = playerName;
                settingsNameInput.text = playerName;

                Debug.Log("Player name set to: " + playerName);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to set player name: " + e);

                errorText.text = "Failed to set name. Check the Console.";
            }
        }

        public void OpenSettingsNamePanel()
        {
            settingsNameInput.text = PlayerPrefs.GetString(PlayerNameKey, "");
            settingsErrorText.text = "";

            settingsNamePanel.SetActive(true);
        }

        public void RemoveSavedName()
        {
            PlayerPrefs.DeleteKey(PlayerNameKey);
            PlayerPrefs.Save();

            Debug.Log("Saved player name removed.");
        }

    }
}