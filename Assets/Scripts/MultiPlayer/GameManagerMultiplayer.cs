using Iterations.Events;
using Unity.Netcode;
using UnityEngine;

public class GameManagerMultiplayer : NetworkBehaviour
{
    private enum State
    {
        WaitingToStart,
        CountdownStart,
        GamePlaying,
        GameOver,
    }

    private NetworkVariable<State> state = new NetworkVariable<State>(State.WaitingToStart);

    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject[] playerPrefabs;
    [SerializeField] private CloningSystemMultiPlayer[] cloningSystems;

    private NetworkVariable<float> gamePlayingTimer = new NetworkVariable<float>(0f);
    [SerializeField] private float gamePlayingTimerMax = 60f;

    private NetworkVariable<float> countdownToStartTimer = new NetworkVariable<float>(3f);
    // player 1 Data
    private int player1Score;
    // player 2 Data
    private int player2Score;

    [Header("Event channels")]
    // player 1
    [SerializeField] private VoidEventChannelSO OnLoseTriggered1;
    [SerializeField] private IntEventChannelSO OnPickUpCollected1;
    // player 2
    [SerializeField] private VoidEventChannelSO OnLoseTriggered2;
    [SerializeField] private IntEventChannelSO OnPickUpCollected2;

    private void Awake()
    {
        // Only execution on the server/host matters for spawning
        if (!IsServer) return;

        // Subscribe to the scene load completed event
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        }

        OnLoseTriggered1.OnEventRaised += OnPlayer1Lose;
        OnPickUpCollected1.OnEventRaised += OnPlayer1Pickup;

        OnLoseTriggered2.OnEventRaised += OnPlayer2Lose;
        OnPickUpCollected2.OnEventRaised += OnPlayer2Pickup;

    }
    //player 1
    private void OnPlayer1Pickup(int obj)
    {
        
    }

    private void OnPlayer1Lose()
    {
        
    } 

    //player 2
    private void OnPlayer2Pickup(int obj)
    {
        
    }

    private void OnPlayer2Lose()
    {
        
    }

    private void OnSceneLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> timeoutClients)
    {
        // Loop over every client that loaded into the scene and spawn their character
        foreach (ulong clientId in clientsCompleted)
        {
            SpawnPlayerForClient(clientId);
        }
    }

    // THIS IS YOUR CUSTOM FUNCTION
    private void SpawnPlayerForClient(ulong clientId)
    {
        // Pick a spawn point (Host gets index 0, Client gets index 1)
        int Index = (int)clientId % spawnPoints.Length;
        Vector3 position = spawnPoints[Index].position;
        Quaternion rotation = spawnPoints[Index].rotation;

        // 1. Instantiate the prefab locally on the Server
        GameObject playerInstance = Instantiate(playerPrefabs[Index], position, rotation);

        // 2. Spawn it across the network and assign ownership to the client
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        // 3. setup cloning systems
        cloningSystems[Index].player = playerInstance.transform;
    }

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

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
                    state.Value = State.GameOver;
                }
                break;

            case State.GameOver:
                break;
        }
    }
}
