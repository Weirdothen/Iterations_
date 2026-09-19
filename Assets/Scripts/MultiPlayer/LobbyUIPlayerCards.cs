using LightSide;
using Unity.Netcode;
using UnityEngine;
// If UniText lives in a namespace, add its "using" line here.

/// <summary>
/// Attach this to your BeforeStartMultiplayerScene canvas.
/// Displays the Relay Join Code and two player cards (Player 1 / Player 2)
/// that update automatically whenever a player joins, leaves, or toggles ready.
///
/// HOW TO SET UP IN THE INSPECTOR:
///   - joinCodeText        → the UniText that shows the room label + 6-char relay code (Host only)
///   - joinCodePanel       → parent GameObject to hide for non-hosts
///   - player1Card / player2Card  → the two player slot root GameObjects
///   - p1StatusText / p2StatusText → shows the waiting text or the player label
///   - p1ReadyText / p2ReadyText   → shows "" or the ready / not-ready text
/// </summary>
public class LobbyUIPlayerCards : MonoBehaviour
{
    [Header("Join Code Display (Host only)")]
    [Tooltip("Set active only for the host. Shows the code they share with their friend.")]
    [SerializeField] private GameObject joinCodePanel;
    [SerializeField] private UniText joinCodeText;

    [Header("Player 1 Card (Host slot)")]
    [SerializeField] private GameObject player1Card;
    [SerializeField] private UniText p1StatusText;
    [SerializeField] private UniText p1ReadyText;

    [Header("Player 2 Card (Client slot)")]
    [SerializeField] private GameObject player2Card;
    [SerializeField] private UniText p2StatusText;
    [SerializeField] private UniText p2ReadyText;

    // ─── Arabic Texts ─────────────────────────────────────────────────────────

    private const string RoomCodeLabel = "كود الغرفة:";
    private const string Player1Label = "اللاعب الأول";
    private const string Player2Label = "اللاعب الثاني";
    private const string WaitingText = "في انتظار اللاعب..";
    private const string ReadyText = "مستعد";
    private const string NotReadyText = "لم استعد";

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        // Show / hide the Join Code panel (only the host sees it)
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        if (joinCodePanel != null) joinCodePanel.SetActive(isHost);

        if (isHost && joinCodeText != null && MultiplayerRelayManager.Instance != null)
        {
            joinCodeText.Text = RoomCodeLabel + " " + MultiplayerRelayManager.Instance.JoinCode;
        }

        // Subscribe to player list changes
        if (CharacterSelectReady.Instance != null)
        {
            CharacterSelectReady.Instance.LobbyPlayers.OnListChanged += OnLobbyPlayersChanged;
        }
        else
        {
            Debug.LogWarning("[LobbyUI] CharacterSelectReady.Instance is null — cards won't update.");
        }

        // Draw the initial state (e.g. host is already in the list on Start)
        RefreshAllCards();
    }

    private void OnDestroy()
    {
        if (CharacterSelectReady.Instance != null)
        {
            CharacterSelectReady.Instance.LobbyPlayers.OnListChanged -= OnLobbyPlayersChanged;
        }
    }

    // ─── Event Handler ────────────────────────────────────────────────────────

    private void OnLobbyPlayersChanged(NetworkListEvent<LobbyPlayerState> changeEvent)
    {
        RefreshAllCards();
    }

    // ─── Card Rendering ───────────────────────────────────────────────────────

    private void RefreshAllCards()
    {
        if (CharacterSelectReady.Instance == null) return;

        var players = CharacterSelectReady.Instance.LobbyPlayers;

        // Slot 0 = Player 1 (Host), Slot 1 = Player 2 (Client)
        SetCard(player1Card, p1StatusText, p1ReadyText, Player1Label, players.Count > 0 ? (LobbyPlayerState?)players[0] : null);
        SetCard(player2Card, p2StatusText, p2ReadyText, Player2Label, players.Count > 1 ? (LobbyPlayerState?)players[1] : null);
    }

    private void SetCard(
        GameObject card,
        UniText statusText,
        UniText readyText,
        string playerLabel,
        LobbyPlayerState? state)
    {
        if (card == null) return;

        if (state == null)
        {
            // Slot is empty
            if (statusText != null) statusText.Text = WaitingText;
            if (readyText != null) readyText.Text = string.Empty;
        }
        else
        {
            // Slot is filled
            if (statusText != null) statusText.Text = playerLabel;
            if (readyText != null) readyText.Text = state.Value.IsReady ? ReadyText : NotReadyText;
        }
    }
}