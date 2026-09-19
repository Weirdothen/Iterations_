using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the arena-selection buttons and the local ready button label.
/// Now subscribes to the NetworkList.OnListChanged event on CharacterSelectReady
/// instead of the old OnAnyReadyStateChanged event (which has been removed).
/// </summary>
public class ArenaSelectoinUi : MonoBehaviour
{
    private Outline[] buttonoutlines;

    [SerializeField] private TextMeshProUGUI readyButtonText;

    void Start()
    {
        buttonoutlines = new Outline[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            buttonoutlines[i] = transform.GetChild(i).GetComponent<Outline>();
            int index = i;

            if (CharacterSelectReady.Instance.IsPartyLeader)
            {
                transform.GetChild(i).GetComponent<Button>().onClick.AddListener(() =>
                {
                    DisableOtherOutlines(index);
                    CharacterSelectReady.Instance.SelectArena(index);
                });
            }
            else
            {
                transform.GetChild(i).GetComponent<Button>().interactable = false;
            }
        }

        DisableOtherOutlines(CharacterSelectReady.Instance.SelectedArenaIndex);

        // Subscribe to the NetworkList change event — fires on join, leave, and ready toggles
        CharacterSelectReady.Instance.LobbyPlayers.OnListChanged += OnLobbyPlayersChanged;

        // Subscribe to the Arena Index change event
        CharacterSelectReady.Instance.OnArenaIndexChanged += OnArenaIndexChanged;

        readyButtonText.SetText("Not Ready");
    }

    private void OnDestroy()
    {
        if (CharacterSelectReady.Instance != null)
        {
            CharacterSelectReady.Instance.LobbyPlayers.OnListChanged -= OnLobbyPlayersChanged;
            CharacterSelectReady.Instance.OnArenaIndexChanged -= OnArenaIndexChanged;
        }
    }

    private void OnLobbyPlayersChanged(NetworkListEvent<LobbyPlayerState> changeEvent)
    {
        // Update the local ready button label
        bool localReady = CharacterSelectReady.Instance.IsPlayerReady(NetworkManager.Singleton.LocalClientId);
        readyButtonText.SetText(localReady ? "Ready" : "Not Ready");
    }

    private void OnArenaIndexChanged(int newIndex)
    {
        // Refresh the arena outline to match the new server-side arena choice
        DisableOtherOutlines(newIndex);
    }

    private void DisableOtherOutlines(int activeOutline)
    {
        for (int i = 0; i < buttonoutlines.Length; i++)
        {
            buttonoutlines[i].enabled = (i == activeOutline);
        }
    }
}
