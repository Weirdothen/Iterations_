using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using TMPro;
using System.Threading.Tasks;

namespace Iterations.Core
{
    public class UGSManager : MonoBehaviour
    {
        public static UGSManager Instance { get; private set; }

        public bool IsSignedIn { get; private set; }

        [Header("Name Setup")]
        [SerializeField] private GameObject namePanel;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_Text errorText;

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

                namePanel.SetActive(true);
            }
            catch (System.Exception e)
            {
                Debug.LogError("UGS initialization failed: " + e);
            }
        }

        public async void SubmitName()
        {
            string playerName = nameInput.text.Trim();

            if (string.IsNullOrEmpty(playerName))
            {
                errorText.text = "Please enter a name.";
                return;
            }

            if (playerName.Length > 20)
            {
                errorText.text = "Name must be 20 characters or less.";
                return;
            }

            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

                Debug.Log("Player name set to: " + playerName);

                namePanel.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to set player name: " + e);
                errorText.text = "Failed to set name. Check the Console.";
            }
        }
    }
}