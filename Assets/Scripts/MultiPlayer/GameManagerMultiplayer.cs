using System;
using Iterations.Events;
using Unity.Netcode;
using UnityEngine;

public class GameManagerMultiplayer : NetworkBehaviour
{
    public static GameManagerMultiplayer Instance { get; private set; }

    public enum State
    {
        WaitingToStart,
        CountdownStart,
        GamePlaying,
        GameOver,
    }

    // UI Event Hooks
    public event Action<State> OnStateChanged;
    public event Action<float> OnTimerUpdated;
    public event Action<int, int> OnScoreUpdated;
    public event Action<ulong, bool, string> OnGameOverEvent; // winnerId, isDraw, reason

    [Header("Scenes")]
    [SerializeField] private string lobbySceneName = "CharacterSelectScene";

    private NetworkVariable<State> state = new NetworkVariable<State>(State.WaitingToStart);
    private NetworkVariable<ulong> partyLeaderId = new NetworkVariable<ulong>(0);
    public ulong PartyLeaderId => partyLeaderId.Value;

    [Header("Player Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject[] playerPrefabs;
    [SerializeField] private CloningSystemMultiPlayer[] cloningSystems;

    [Header("Coin Spawning")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private Transform[] coinSpawnPoints;
    [SerializeField] private float coinSpawnTimerMin = 2f;
    [SerializeField] private float coinSpawnTimerMax = 5f;
    private float currentCoinSpawnTimer;
    private System.Collections.Generic.Dictionary<Transform, GameObject> activeCoins = new System.Collections.Generic.Dictionary<Transform, GameObject>();

    [Header("Timers")]
    private NetworkVariable<float> gamePlayingTimer = new NetworkVariable<float>(0f);
    [SerializeField] private float gamePlayingTimerMax = 60f;

    private NetworkVariable<float> countdownToStartTimer = new NetworkVariable<float>(3f);
    
    // Player Scores
    private NetworkVariable<int> player1Score = new NetworkVariable<int>(0);
    private NetworkVariable<int> player2Score = new NetworkVariable<int>(0);

    // Player Status
    private bool isPlayer1Dead = false;
    private bool isPlayer2Dead = false;

    // Spawn index counter – increments per player spawned, independent of clientId
    // This avoids the clientId % length bug when clients reconnect and get higher IDs
    private int _spawnCounter = 0;

    [Header("Event channels")]
    // player 1
    [SerializeField] private VoidEventChannelSO OnLoseTriggered1;
    [SerializeField] private IntEventChannelSO OnPickUpCollected1;
    // player 2
    [SerializeField] private VoidEventChannelSO OnLoseTriggered2;
    [SerializeField] private IntEventChannelSO OnPickUpCollected2;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // Wire up UI events to NetworkVariables
        state.OnValueChanged += (State previousValue, State newValue) => 
        {
            if(newValue == State.GamePlaying)
            {
                for(int i = 0;i< cloningSystems.Length; i++)
                {
                    cloningSystems[i].StartRun();
                }
            }
            OnStateChanged?.Invoke(newValue);
        };
        
        gamePlayingTimer.OnValueChanged += (float prev, float curr) =>
        {
            if (state.Value == State.GamePlaying)
                OnTimerUpdated?.Invoke(curr);
        };

        countdownToStartTimer.OnValueChanged += (float prev, float curr) =>
        {
            if (state.Value == State.CountdownStart)
                OnTimerUpdated?.Invoke(curr);
        };

        player1Score.OnValueChanged += (int prev, int curr) => OnScoreUpdated?.Invoke(curr, player2Score.Value);
        player2Score.OnValueChanged += (int prev, int curr) => OnScoreUpdated?.Invoke(player1Score.Value, curr);

        // Only Server handles logic binding
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            OnLoseTriggered1.OnEventRaised += OnPlayer1Lose;
            OnPickUpCollected1.OnEventRaised += OnPlayer1Pickup;

            OnLoseTriggered2.OnEventRaised += OnPlayer2Lose;
            OnPickUpCollected2.OnEventRaised += OnPlayer2Pickup;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

            OnLoseTriggered1.OnEventRaised -= OnPlayer1Lose;
            OnPickUpCollected1.OnEventRaised -= OnPlayer1Pickup;

            OnLoseTriggered2.OnEventRaised -= OnPlayer2Lose;
            OnPickUpCollected2.OnEventRaised -= OnPlayer2Pickup;
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (state.Value == State.GamePlaying)
        {
            // Forfeit! Remaining player wins
            ulong winnerId = clientId == 0 ? 1ul : 0ul;
            EndGame(winnerId, false, "Player Disconnected (Forfeit)");
        }
    }

    private void EndGame(ulong winnerId, bool isDraw, string reason)
    {
        if (state.Value == State.GameOver) return;

        state.Value = State.GameOver;
        EndGameClientRpc(winnerId, isDraw, reason);
    }

    [ClientRpc]
    private void EndGameClientRpc(ulong winnerId, bool isDraw, string reason)
    {
        OnGameOverEvent?.Invoke(winnerId, isDraw, reason);
        Debug.Log($"Game Over! Draw: {isDraw}, Winner ID: {winnerId}, Reason: {reason}");
    }

    private void OnPlayer1Pickup(int obj)
    {
        if (isPlayer1Dead || state.Value != State.GamePlaying) return;
        player1Score.Value++;
        CheckWinConditionOnScoreChange();
    }

    private void OnPlayer1Lose()
    {
        if (isPlayer1Dead || state.Value != State.GamePlaying) return;
        isPlayer1Dead = true;
        Debug.Log("Player 1 died!");

        if (player2Score.Value > player1Score.Value)
        {
            EndGame(1, false, "Player 1 died while trailing in score.");
        }
        else
        {
            if (isPlayer2Dead)
            {
                CheckTimerEndVictory();
            }
        }
    }

    private void OnPlayer2Pickup(int obj)
    {
        if (isPlayer2Dead || state.Value != State.GamePlaying) return;
        player2Score.Value++;
        CheckWinConditionOnScoreChange();
    }

    private void OnPlayer2Lose()
    {
        if (isPlayer2Dead || state.Value != State.GamePlaying) return;
        isPlayer2Dead = true;
        Debug.Log("Player 2 died!");

        if (player1Score.Value > player2Score.Value)
        {
            EndGame(0, false, "Player 2 died while trailing in score.");
        }
        else
        {
            if (isPlayer1Dead)
            {
                CheckTimerEndVictory();
            }
        }
    }

    private void CheckWinConditionOnScoreChange()
    {
        if (state.Value != State.GamePlaying) return;

        if (isPlayer1Dead && player2Score.Value > player1Score.Value)
        {
            EndGame(1, false, "Player 2 surpassed Player 1's score after P1 died.");
        }
        else if (isPlayer2Dead && player1Score.Value > player2Score.Value)
        {
            EndGame(0, false, "Player 1 surpassed Player 2's score after P2 died.");
        }
    }

    private void CheckTimerEndVictory()
    {
        if (player1Score.Value > player2Score.Value)
        {
            EndGame(0, false, "Time up! Player 1 wins on score.");
        }
        else if (player2Score.Value > player1Score.Value)
        {
            EndGame(1, false, "Time up! Player 2 wins on score.");
        }
        else
        {
            EndGame(0, true, "Time up! It's a draw.");
        }
    }

    private void OnSceneLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> timeoutClients)
    {
        // Determine the Party Leader (first actual player, ignoring headless dedicated server)
        foreach (ulong clientId in clientsCompleted)
        {
            if (clientId == NetworkManager.ServerClientId && !NetworkManager.Singleton.IsHost) continue;
            partyLeaderId.Value = clientId;
            break;
        }

        // Reset spawn counter and game state for a fresh round
        _spawnCounter = 0;
        isPlayer1Dead = false;
        isPlayer2Dead = false;
        player1Score.Value = 0;
        player2Score.Value = 0;
        activeCoins.Clear();

        foreach (ulong clientId in clientsCompleted)
        {
            SpawnPlayerForClient(clientId);
        }

        currentCoinSpawnTimer = 0f; // Spawn a coin immediately when game starts
        state.Value = State.CountdownStart;
    }

    private void SpawnCoinRandomly()
    {
        if (coinPrefab == null || coinSpawnPoints == null || coinSpawnPoints.Length == 0) return;
        
        // Try a few times to find an empty spot
        int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            int index = UnityEngine.Random.Range(0, coinSpawnPoints.Length);
            Transform point = coinSpawnPoints[index];
            
            // If the dictionary has this point, and the GameObject hasn't been destroyed yet, it's occupied.
            if (activeCoins.ContainsKey(point) && activeCoins[point] != null)
            {
                continue; // Try again
            }

            GameObject coin = Instantiate(coinPrefab, point.position, point.rotation);
            NetworkObject netObj = coin.GetComponent<NetworkObject>();

            

            netObj.Spawn(true);
            
            // Track the newly spawned coin at this location
            activeCoins[point] = coin;
            return;
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
       
        int index = _spawnCounter % spawnPoints.Length;
        _spawnCounter++;

        Vector3 position = spawnPoints[index].position;
        Quaternion rotation = spawnPoints[index].rotation;

        GameObject playerInstance = Instantiate(playerPrefabs[index], position, rotation);
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        cloningSystems[index].player = playerInstance.transform;
        
    }

    public void ReturnToLobby()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else if(NetworkManager.Singleton.LocalClientId == PartyLeaderId)
        {
            ReturnToLobbyServerRpc();
        }
        else
        {
            Debug.Log("just the leader can back to the lobby");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReturnToLobbyServerRpc()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void Update()
    {
        if (!IsServer) return;

        switch (state.Value)
        {
            case State.WaitingToStart:
                break;

            case State.CountdownStart:
                countdownToStartTimer.Value -= Time.deltaTime;
                if (countdownToStartTimer.Value < 0f)
                {
                    state.Value = State.GamePlaying;
                    gamePlayingTimer.Value = gamePlayingTimerMax;
                }
                break;

            case State.GamePlaying:
                gamePlayingTimer.Value -= Time.deltaTime;
                if (gamePlayingTimer.Value < 0f)
                {
                    CheckTimerEndVictory();
                }
                else
                {
                    // Handle Coin Spawning
                    currentCoinSpawnTimer -= Time.deltaTime;
                    if (currentCoinSpawnTimer <= 0f)
                    {
                        SpawnCoinRandomly();
                        currentCoinSpawnTimer = UnityEngine.Random.Range(coinSpawnTimerMin, coinSpawnTimerMax);
                    }
                }
                break;

            case State.GameOver:
                break;
        }
    }
    public State GetCurrentStat()
    {
        return state.Value;
    }
}
