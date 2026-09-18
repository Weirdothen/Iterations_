using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
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

    private const string RELAY_JOIN_CODE_KEY = "RelayJoinCode";
    private const float HEARTBEAT_INTERVAL = 15f;
    private float heartbeatTimer;

    private Lobby currentLobby;

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
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[Relay] Host Join Code: {relayCode}");

            // 3. Build RelayServerData manually — compatible with com.unity.services.multiplayer
            //    which bundles relay without the convenience constructor from the standalone package.
            var relayServerData = AllocationUtils.ToRelayServerData(allocation, RelayProtocol.UDP);


            // 4. Hand the data to the UnityTransport component on the NetworkManager
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                          .SetRelayServerData(relayServerData);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = CreatePlayer(),
                Data = new Dictionary<string, DataObject>
                    {
                        { RELAY_JOIN_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(
            $"player1's Lobby",
                maxConnections,
                options
            );
            JoinCode = currentLobby.LobbyCode;

            Debug.Log($"[LobbyServiceManager] Lobby created: {currentLobby.LobbyCode}");

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
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = CreatePlayer(),
            };

            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode, options);
            // Get Relay join code from lobby data
            string relayJoinCode = currentLobby.Data[RELAY_JOIN_CODE_KEY].Value;
            // 1. Retrieve the Relay allocation from the join code
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            // 2. Build RelayServerData manually for the client
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, RelayProtocol.UDP);
            transport.SetRelayServerData(relayServerData);

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
    private Player CreatePlayer()
    {
        return new Player();
    }
    private bool IsHost()
    {
        if (currentLobby == null) return false;
        return currentLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
    private void HandleHeartbeat()
    {
        if (currentLobby == null || !IsHost()) return;

        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer <= 0f)
        {
            heartbeatTimer = HEARTBEAT_INTERVAL;
            LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
        }
    }


}
