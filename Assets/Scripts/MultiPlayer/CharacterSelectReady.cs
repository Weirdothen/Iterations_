using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    /// <summary>Fires whenever any player's ready state changes so the UI can refresh.</summary>
    public event Action OnAnyReadyStateChanged;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------
    private Dictionary<ulong, bool> playerReadyDictionary = new Dictionary<ulong, bool>();

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
    }

    public override void OnNetworkSpawn()
    {
        // Hook UI to NetworkVariable changes (runs on all clients)
        partyLeaderId.OnValueChanged += (prev, curr) =>
        {
            bool amLeader = NetworkManager.Singleton.LocalClientId == curr;
            OnPartyLeaderStatusReceived?.Invoke(amLeader);
        };

        selectedArenaIndex.OnValueChanged += (prev, curr) =>
        {
            // Notify UI that arena changed (optional, hook in your UI)
            OnAnyReadyStateChanged?.Invoke();
        };

        if (!IsServer) return;

        // -------------------------------------------------------
        // Determine Party Leader on the server
        // Skip the headless dedicated server's own client ID
        // -------------------------------------------------------
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            bool isHeadlessServer = clientId == NetworkManager.ServerClientId && !NetworkManager.Singleton.IsHost;
            if (isHeadlessServer) continue;

            partyLeaderId.Value = clientId; // First real player = leader
            break;
        }
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

        // Toggle state
        bool currentState = playerReadyDictionary.ContainsKey(senderId) && playerReadyDictionary[senderId];
        bool newState = !currentState;
        playerReadyDictionary[senderId] = newState;

        // Sync to all clients so their UI can update indicators
        SyncReadyStateClientRpc(senderId, newState);

        // Check if exactly 2 players are connected and all are ready
        if (NetworkManager.Singleton.ConnectedClientsIds.Count != 2) return;

        bool allReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SelectedArenaName, LoadSceneMode.Single);
        }
    }

    [ClientRpc]
    private void SyncReadyStateClientRpc(ulong clientId, bool isReady)
    {
        playerReadyDictionary[clientId] = isReady;

        // Notify UI that something changed
        OnAnyReadyStateChanged?.Invoke();

        // If this was the local player, also fire the personal event
        if (clientId == NetworkManager.Singleton.LocalClientId)
            OnLocalReadyStateChanged?.Invoke(isReady);
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
        Debug.Log("sent");
        SelectArenaServerRpc(arenaIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectArenaServerRpc(int arenaIndex)
    {
        int clamped = Mathf.Clamp(arenaIndex, 0, Mathf.Max(0, arenaSceneNames.Length - 1));
        selectedArenaIndex.Value = clamped;
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------
    public bool IsPlayerReady(ulong clientId)
    {
        return playerReadyDictionary.ContainsKey(clientId) && playerReadyDictionary[clientId];
    }
}