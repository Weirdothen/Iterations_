using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages Unity Relay allocation for Host and Client flows.
///
/// DESIGNED FOR EASY EXTENSION:
/// If you later add Unity Lobbies (public server browser), this class is where
/// you create/join the Lobby alongside the Relay allocation.
/// The method signatures remain the same, so callers (MainMenuManager) won't need to change.
///
/// HOW TO USE:
///   Host   → await MultiplayerRelayManager.Instance.CreateRelayAndHost();
///             Read MultiplayerRelayManager.Instance.JoinCode to display to the user.
///
///   Client → await MultiplayerRelayManager.Instance.JoinRelayAndConnect(code);
/// </summary>
public class MultiplayerRelayManager : MonoBehaviour
{
    public static MultiplayerRelayManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("The scene to load after the host successfully starts.")]
    [SerializeField] private string lobbySceneName = "BeforeStartMultiplayerScene";

    [Tooltip("Maximum number of players (1 host + N-1 clients).")]
    [SerializeField] private int maxConnections = 2;

    /// <summary>The Relay Join Code generated when hosting. Display this in your UI.</summary>
    public string JoinCode { get; private set; } = string.Empty;

    /// <summary>True while a host/join operation is in progress (use to disable buttons).</summary>
    public bool IsBusy { get; private set; } = false;

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

    // -------------------------------------------------------------------------
    // HOST FLOW
    // -------------------------------------------------------------------------

    /// <summary>
    /// Allocates a Relay server, stores the Join Code, configures the transport,
    /// starts the Host, then loads the lobby scene for all players.
    /// </summary>
    /// <returns>True on success, false on failure.</returns>
    public async Task<bool> CreateRelayAndHost()
    {
        if (IsBusy) return false;
        IsBusy = true;
        JoinCode = string.Empty;

        try
        {
            // 1. Allocate a Relay server slot (maxConnections - 1 = number of joining clients)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections - 1);

            // 2. Get the human-readable join code
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[Relay] Host Join Code: {JoinCode}");

            // 3. Build RelayServerData manually — compatible with com.unity.services.multiplayer
            //    which bundles relay without the convenience constructor from the standalone package.
            RelayServerData relayServerData = BuildRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.ConnectionData,
                allocation.ConnectionData,   // host uses its own connection data as the "host" endpoint
                allocation.Key,
                isSecure: true               // true = DTLS (encrypted), false = UDP (unencrypted)
            );

            // 4. Hand the data to the UnityTransport component on the NetworkManager
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                          .SetRelayServerData(relayServerData);

            // 5. Start hosting
            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("[Relay] NetworkManager.StartHost() failed.");
                IsBusy = false;
                return false;
            }

            // 6. Load the lobby/character-select scene for all connected clients
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);

            IsBusy = false;
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] CreateRelayAndHost failed: {e}");
            IsBusy = false;
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // CLIENT FLOW
    // -------------------------------------------------------------------------

    /// <summary>
    /// Joins an existing Relay allocation using a Join Code, configures the transport,
    /// and starts the client connection.
    /// </summary>
    /// <param name="joinCode">The 6-character code shared by the host.</param>
    /// <returns>True on success, false on failure.</returns>
    public async Task<bool> JoinRelayAndConnect(string joinCode)
    {
        if (IsBusy) return false;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogError("[Relay] Join Code is empty.");
            return false;
        }

        IsBusy = true;

        try
        {
            // 1. Retrieve the Relay allocation from the join code
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim().ToUpper());

            // 2. Build RelayServerData manually for the client
            //    Note: HostConnectionData is different from our own ConnectionData here.
            RelayServerData relayServerData = BuildRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,   // the host's endpoint — different for clients
                joinAllocation.Key,
                isSecure: true
            );

            // 3. Configure the transport
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                          .SetRelayServerData(relayServerData);

            // 4. Start the client
            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("[Relay] NetworkManager.StartClient() failed.");
                IsBusy = false;
                return false;
            }

            IsBusy = false;
            return true;
        }
        catch (System.Exception e)
        {
            // Surface a meaningful failure reason via the popup system
            ConnectionFailedData.HasMessage = true;
            ConnectionFailedData.Message    = $"Failed to join: {e.Message}";
            Debug.LogError($"[Relay] JoinRelayAndConnect failed: {e}");
            IsBusy = false;
            return false;
        }

        // FUTURE EXTENSION POINT FOR UNITY LOBBIES:
        // Before step 1, call LobbyService.Instance.JoinLobbyByCodeAsync(joinCode)
        // to join the Lobby entry, then read the Relay join code from lobby data.
        // The rest of the flow below stays identical.
    }

    // -------------------------------------------------------------------------
    // Helper — manual RelayServerData construction
    // -------------------------------------------------------------------------

    /// <summary>
    /// Constructs a <see cref="RelayServerData"/> from raw allocation bytes.
    /// This avoids the convenience constructor that only existed in the
    /// standalone com.unity.services.relay package and is not available when
    /// using com.unity.services.multiplayer.
    /// </summary>
    private static RelayServerData BuildRelayServerData(
        string  host,
        ushort  port,
        byte[]  allocationIdBytes,
        byte[]  connectionData,
        byte[]  hostConnectionData,
        byte[]  key,
        bool    isSecure)
    {
        return new RelayServerData(
            host,
            port,
            allocationIdBytes,
            connectionData,
            hostConnectionData,
            key,
            isSecure);
    }
}
