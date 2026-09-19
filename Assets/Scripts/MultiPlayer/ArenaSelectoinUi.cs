using LightSide;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
// If UniText lives in a namespace, add its "using" line here.

/// <summary>
/// Manages the arena-selection buttons and the local ready button (label + color).
/// Subscribes to the NetworkList.OnListChanged event on CharacterSelectReady.
/// </summary>
public class ArenaSelectoinUi : MonoBehaviour
{
    private Outline[] buttonoutlines;

    [Header("Ready Button")]
    [SerializeField] private UniText readyButtonText;
    [Tooltip("The Image component on the ready button (its color changes red/green)")]
    [SerializeField] private Image readyButtonImage;

    [SerializeField] private Color notReadyColor = Color.red;
    [SerializeField] private Color readyColor = Color.green;

    private const string ReadyText = "مستعد";
    private const string NotReadyText = "لم استعد";

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

        // Start in the "not ready" look
        UpdateReadyButtonVisuals(false);
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
        // Update the local ready button text + color
        bool localReady = CharacterSelectReady.Instance.IsPlayerReady(NetworkManager.Singleton.LocalClientId);
        UpdateReadyButtonVisuals(localReady);

        // Refresh the arena outline to match any server-side arena change
        DisableOtherOutlines(CharacterSelectReady.Instance.SelectedArenaIndex);
    }

    private void UpdateReadyButtonVisuals(bool isReady)
    {
        if (readyButtonText != null)
        {
            readyButtonText.Text = isReady ? ReadyText : NotReadyText;
        }

        if (readyButtonImage != null)
        {
            readyButtonImage.color = isReady ? readyColor : notReadyColor;
        }
    }

    private void DisableOtherOutlines(int activeOutline)
    {
        for (int i = 0; i < buttonoutlines.Length; i++)
        {
            buttonoutlines[i].enabled = (i == activeOutline);
        }
    }
}