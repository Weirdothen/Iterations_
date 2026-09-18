using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to your NetworkManager GameObject.
/// Handles connection approval (room-full checks, etc.) and
/// notifies the UI via the static ConnectionFailedData class
/// when a local connection attempt fails.
///
/// IMPORTANT: ConnectionApprovalCallback is a property on NGO, not a true C# event.
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
        // If another instance already exists, destroy this one and bail out.
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

        // Direct assignment (=) instead of += because ConnectionApprovalCallback
        // is a property that only allows one handler. This also naturally
        // overwrites any stale handler from a previous session.
        NetworkManager.Singleton.ConnectionApprovalCallback = OnConnectionApproval;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        // Clear the property entirely when we disable
        NetworkManager.Singleton.ConnectionApprovalCallback = null;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
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
        // Count current real players (exclude dedicated server client ID)
        int currentPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;

        if (currentPlayers >= maxPlayers)
        {
            // Deny – room is full
            response.Approved = false;
            response.Reason = "Room is full. Maximum 2 players allowed.";
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = false; // GameManagerMultiplayer handles spawning
    }

    // -------------------------------------------------------------------------
    // Client: handle being disconnected / rejected
    // -------------------------------------------------------------------------
    private void OnClientDisconnect(ulong clientId)
    {
        // Only react to our own local client being disconnected
        if (!IsLocalClientDisconnect(clientId)) return;

        // If we are still in the game scene (not already on the menu), navigate back
        if (SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            // The GameManagerMultiplayer already handles the "forfeit" for the remaining player.
            // Here we just handle the player who GOT disconnected.
            ConnectionFailedData.HasMessage = true;
            ConnectionFailedData.Message = "You lost connection to the session.";
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private bool IsLocalClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
            return false; // We are a dedicated server, not a player

        return clientId == NetworkManager.Singleton.LocalClientId ||
               (!NetworkManager.Singleton.IsConnectedClient && clientId == 0);
    }
}

// -------------------------------------------------------------------------
// Static bag to pass a failure reason across scene loads without DontDestroy
// -------------------------------------------------------------------------
public static class ConnectionFailedData
{
    public static bool HasMessage = false;
    public static string Message = "";
}
