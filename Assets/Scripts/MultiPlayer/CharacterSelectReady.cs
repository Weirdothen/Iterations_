using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the lobby state (player join/leave, ready-up, arena selection).
/// Uses NetworkList<LobbyPlayerState> to sync player states to all clients automatically.
///
/// HOW TO EXTEND:
///   - Add fields to LobbyPlayerState.cs.
///   - Add a new Server RPC here (e.g. SetPlayerNameServerRpc) that finds the entry
///     in LobbyPlayers and replaces it with an updated struct.
/// </summary>
public class CharacterSelectReady : NetworkBehaviour
{
    public static CharacterSelectReady Instance { get; private set; }

    // -------------------------------------------------------------------------
    // UI Event Hooks (subscribe from your Lobby UI script)
    // -------------------------------------------------------------------------

    /// <summary>Fires whenever the local player's ready state changes (true = ready).</summary>
    public event Action<bool> OnLocalReadyStateChanged;

    /// <summary>Fires when the party leader status is confirmed (true = I am the leader).</summary>
    public event Action<bool> OnPartyLeaderStatusReceived;

    // -------------------------------------------------------------------------
    // Synced Player List  ← the main extensible state container
    // -------------------------------------------------------------------------

    /// <summary>
    /// The authoritative, server-managed list of all players currently in the lobby.
    /// Subscribe to LobbyPlayers.OnListChanged to react to join/leave/ready changes.
    /// </summary>
    public NetworkList<LobbyPlayerState> LobbyPlayers { get; private set; }

    // Party Leader – synced so all clients know who the leader is
    private NetworkVariable<ulong> partyLeaderId = new NetworkVariable<ulong>(ulong.MaxValue);
    public ulong PartyLeaderId => partyLeaderId.Value;
    public bool IsPartyLeader => IsSpawned &&
                                  NetworkManager.Singleton != null &&
                                  NetworkManager.Singleton.LocalClientId == partyLeaderId.Value;

    // Arena selection – leader picks an index, server validates and syncs it
    [Header("Arena Scenes")]
    [Tooltip("Fill with exact scene names in Build Settings, e.g. Arena1, Arena2")]
    [SerializeField] private string[] arenaSceneNames = { "MultiplayerScene" };

    private NetworkVariable<int> selectedArenaIndex = new NetworkVariable<int>(0);
    public int SelectedArenaIndex => selectedArenaIndex.Value;
    public string SelectedArenaName => (arenaSceneNames != null && arenaSceneNames.Length > selectedArenaIndex.Value)
                                        ? arenaSceneNames[selectedArenaIndex.Value] : "MultiplayerScene";

    // -------------------------------------------------------------------------
    // Unity / Network lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // NetworkList MUST be created in Awake, before OnNetworkSpawn
        LobbyPlayers = new NetworkList<LobbyPlayerState>();
    }

    public override void OnNetworkSpawn()
    {
        // Hook UI to NetworkVariable changes (runs on all clients)
        partyLeaderId.OnValueChanged += (prev, curr) =>
        {
            bool amLeader = NetworkManager.Singleton.LocalClientId == curr;
            OnPartyLeaderStatusReceived?.Invoke(amLeader);
        };

        // When the arena selection changes, let UI scripts know
        selectedArenaIndex.OnValueChanged += (prev, curr) =>
        {
            // LobbyPlayers.OnListChanged listeners will see this indirectly;
            // fire a dummy change so UI refreshes if it only watches the list event.
            // Or, subscribe to selectedArenaIndex directly in your UI if preferred.
        };

        if (IsServer)
        {
            // Server tracks clients joining and leaving
            NetworkManager.Singleton.OnClientConnectedCallback    += Server_OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback   += Server_OnClientDisconnected;

            // Add all currently connected clients (including host and any clients that are already here when reloading the scene)
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (GetPlayerState(clientId) == null)
                {
                    Server_AddPlayer(clientId);
                }
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback    -= Server_OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback   -= Server_OnClientDisconnected;
        }
    }

    // -------------------------------------------------------------------------
    // Server: manage player join / leave
    // -------------------------------------------------------------------------

    private void Server_OnClientConnected(ulong clientId)
    {
        Server_AddPlayer(clientId);
    }

    private void Server_OnClientDisconnected(ulong clientId)
    {
        for (int i = 0; i < LobbyPlayers.Count; i++)
        {
            if (LobbyPlayers[i].ClientId == clientId)
            {
                LobbyPlayers.RemoveAt(i);
                break;
            }
        }
        // Reassign party leader if they left
        Server_RefreshPartyLeader();
    }

    private void Server_AddPlayer(ulong clientId)
    {
        var state = new LobbyPlayerState
        {
            ClientId = clientId,
            IsReady  = false,
            // Initialize new fields here when you extend LobbyPlayerState
        };
        LobbyPlayers.Add(state);

        Server_RefreshPartyLeader();
    }

    private void Server_RefreshPartyLeader()
    {
        // The first real player in the list is the party leader
        foreach (var player in LobbyPlayers)
        {
            bool isHeadlessServer = player.ClientId == NetworkManager.ServerClientId && !NetworkManager.Singleton.IsHost;
            if (isHeadlessServer) continue;

            partyLeaderId.Value = player.ClientId;
            return;
        }
        partyLeaderId.Value = ulong.MaxValue; // No players left
    }

    // -------------------------------------------------------------------------
    // Ready Toggle (call this from your "Ready" button)
    // -------------------------------------------------------------------------

    public void ToggleReady()
    {
        ToggleReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        for (int i = 0; i < LobbyPlayers.Count; i++)
        {
            if (LobbyPlayers[i].ClientId == senderId)
            {
                // Structs in NetworkList must be replaced, not mutated in-place
                var updated = LobbyPlayers[i];
                updated.IsReady = !updated.IsReady;
                LobbyPlayers[i] = updated;

                // Notify the local player's own UI
                NotifyLocalReadyStateClientRpc(senderId, updated.IsReady);
                break;
            }
        }

        Server_CheckAllReady();
    }

    [ClientRpc]
    private void NotifyLocalReadyStateClientRpc(ulong clientId, bool isReady)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            OnLocalReadyStateChanged?.Invoke(isReady);
        }
    }

    private void Server_CheckAllReady()
    {
        if (LobbyPlayers.Count != 2) return;

        foreach (var player in LobbyPlayers)
        {
            if (!player.IsReady) return;
        }

        // All 2 players are ready — load the arena
        NetworkManager.Singleton.SceneManager.LoadScene(SelectedArenaName, LoadSceneMode.Single);
    }

    // -------------------------------------------------------------------------
    // Arena Selection (only the leader should call this from UI)
    // -------------------------------------------------------------------------

    public void SelectArena(int arenaIndex)
    {
        if (!IsPartyLeader)
        {
            Debug.LogWarning("CharacterSelectReady: Non-leader tried to select an arena.");
            return;
        }
        SelectArenaServerRpc(arenaIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectArenaServerRpc(int arenaIndex)
    {
        int clamped = Mathf.Clamp(arenaIndex, 0, Mathf.Max(0, arenaSceneNames.Length - 1));
        selectedArenaIndex.Value = clamped;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    public bool IsPlayerReady(ulong clientId)
    {
        foreach (var player in LobbyPlayers)
        {
            if (player.ClientId == clientId) return player.IsReady;
        }
        return false;
    }

    public LobbyPlayerState? GetPlayerState(ulong clientId)
    {
        foreach (var player in LobbyPlayers)
        {
            if (player.ClientId == clientId) return player;
        }
        return null;
    }
}