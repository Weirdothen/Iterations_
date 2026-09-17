using UnityEngine;
using Unity.Netcode;
using Iterations.Core;

public static class DisconnectReasonData
{
    public static bool HasDisconnectedMessage = false;
    public static string DisconnectMessage = "";
}

public class NetworkDisconnectHandler : MonoBehaviour
{
    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        // Trigger only if we are the client being disconnected
        if (clientId == NetworkManager.Singleton.LocalClientId || clientId == 0) 
        {
            // Set the static data for the main menu to read
            DisconnectReasonData.HasDisconnectedMessage = true;
            DisconnectReasonData.DisconnectMessage = "Lost connection to the host.";
            
            // Return to main menu
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
            }
        }
    }
}
