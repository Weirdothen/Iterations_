using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to your NetworkManager GameObject.
/// Handles connection approval (room-full checks) and notifies the UI
/// via the static ConnectionFailedData class when a local connection attempt fails.
///
/// NOTE: ConnectionApprovalCallback is a property on NGO, not a true C# event.
/// Only one handler can be assigned at a time. We use direct assignment (=)
/// and a singleton guard to prevent double-registration when scenes reload.
/// </summary>
public class NetworkConnectionHandler : MonoBehaviour
{
    // Singleton guard so scene reloads don't re-register
    private static NetworkConnectionHandler _instance;

    [Header("Settings")]
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.ConnectionApprovalCallback  = OnConnectionApproval;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.ConnectionApprovalCallback   = null;
        NetworkManager.Singleton.OnClientDisconnectCallback  -= OnClientDisconnect;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // -------------------------------------------------------------------------
    // Server: approve / deny incoming connection requests
    // -------------------------------------------------------------------------
    private void OnConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        int currentPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;

        if (currentPlayers >= maxPlayers)
        {
            response.Approved = false;
            response.Reason   = "Room is full. Maximum 2 players allowed.";
            return;
        }

        response.Approved            = true;
        response.CreatePlayerObject  = false; // GameManagerMultiplayer handles spawning
    }

    // -------------------------------------------------------------------------
    // Client: handle being disconnected / rejected
    // -------------------------------------------------------------------------
    private void OnClientDisconnect(ulong clientId)
    {
        if (!IsLocalClientDisconnect(clientId)) return;

        // Build a meaningful reason. NGO populates DisconnectReason when the server
        // explicitly denies or kicks a client (e.g. "Room is full").
        string reason = NetworkManager.Singleton.DisconnectReason;
        if (string.IsNullOrEmpty(reason))
        {
            reason = "You lost connection to the session.";
        }

        // Always store the reason — the popup UI will display it wherever it lives.
        ConnectionFailedData.HasMessage = true;
        ConnectionFailedData.Message    = reason;

        bool alreadyOnMenu = SceneManager.GetActiveScene().name == mainMenuSceneName;

        if (!alreadyOnMenu)
        {
            // Disconnected from a game scene — shut down and go back to the menu.
            // The GameManagerMultiplayer already handles the forfeit for the remaining player.
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(mainMenuSceneName);
        }
        // If we are already on the menu (e.g. join was rejected), just let
        // ConnectionFailedPopupUI.Update() pick up the message — no scene load needed.
    }

    private bool IsLocalClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
            return false; // dedicated server, not a player

        return clientId == NetworkManager.Singleton.LocalClientId ||
               (!NetworkManager.Singleton.IsConnectedClient && clientId == 0);
    }
}

// -------------------------------------------------------------------------
// Static bag to pass a failure reason across scene loads without DontDestroy
// -------------------------------------------------------------------------
public static class ConnectionFailedData
{
    public static bool   HasMessage = false;
    public static string Message    = string.Empty;
}
