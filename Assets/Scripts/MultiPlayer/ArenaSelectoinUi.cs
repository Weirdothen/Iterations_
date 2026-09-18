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

        readyButtonText.SetText("Not Ready");
    }

    private void OnDestroy()
    {
        if (CharacterSelectReady.Instance != null)
        {
            CharacterSelectReady.Instance.LobbyPlayers.OnListChanged -= OnLobbyPlayersChanged;
        }
    }

    private void OnLobbyPlayersChanged(NetworkListEvent<LobbyPlayerState> changeEvent)
    {
        // Update the local ready button label
        bool localReady = CharacterSelectReady.Instance.IsPlayerReady(NetworkManager.Singleton.LocalClientId);
        readyButtonText.SetText(localReady ? "Ready" : "Not Ready");

        // Refresh the arena outline to match any server-side arena change
        DisableOtherOutlines(CharacterSelectReady.Instance.SelectedArenaIndex);
    }

    private void DisableOtherOutlines(int activeOutline)
    {
        for (int i = 0; i < buttonoutlines.Length; i++)
        {
            buttonoutlines[i].enabled = (i == activeOutline);
        }
    }
}
