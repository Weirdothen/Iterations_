using LightSide;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
// If UniText lives in a namespace, add its "using" line here.

/// <summary>
/// Manages the arena-selection buttons and the local ready button (label + color).
/// Subscribes to the NetworkList.OnListChanged event on CharacterSelectReady.
/// Every CHILD of this object is treated as one arena button, in order.
/// </summary>
public class ArenaSelectoinUi : MonoBehaviour
{
    private Outline[] buttonoutlines;

    [Header("Arena Outline")]
    [Tooltip("Outline color of the currently selected arena button")]
    [SerializeField] private Color selectedOutlineColor = Color.red;
    [Tooltip("Outline thickness (X, Y). Y is usually the negative of X, e.g. 4 and -4")]
    [SerializeField] private Vector2 outlineThickness = new Vector2(4f, -4f);

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
            Transform child = transform.GetChild(i);

            // Use the button's Outline, or add one automatically if it's missing
            buttonoutlines[i] = child.GetComponent<Outline>();
            if (buttonoutlines[i] == null)
            {
                buttonoutlines[i] = child.gameObject.AddComponent<Outline>();
            }

            int index = i;

            if (CharacterSelectReady.Instance.IsPartyLeader)
            {
                child.GetComponent<Button>().onClick.AddListener(() =>
                {
                    DisableOtherOutlines(index);
                    CharacterSelectReady.Instance.SelectArena(index);
                });
            }
            else
            {
                child.GetComponent<Button>().interactable = false;
            }
        }

        DisableOtherOutlines(CharacterSelectReady.Instance.SelectedArenaIndex);

        // Subscribe to the NetworkList change event — fires on join, leave, and ready toggles
        CharacterSelectReady.Instance.LobbyPlayers.OnListChanged += OnLobbyPlayersChanged;

        // Subscribe to the Arena Index change event
        CharacterSelectReady.Instance.OnArenaIndexChanged += OnArenaIndexChanged;

        // Start in the "not ready" look
        UpdateReadyButtonVisuals(false);
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
        // Update the local ready button text + color
        bool localReady = CharacterSelectReady.Instance.IsPlayerReady(NetworkManager.Singleton.LocalClientId);

        UpdateReadyButtonVisuals(localReady);
    }

    private void OnArenaIndexChanged(int newIndex)
    {
        // Refresh the arena outline to match the new server-side arena choice
        DisableOtherOutlines(newIndex);
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
            if (buttonoutlines[i] == null) continue;

            buttonoutlines[i].effectColor = selectedOutlineColor;
            buttonoutlines[i].effectDistance = outlineThickness;
            buttonoutlines[i].enabled = (i == activeOutline);
        }
    }
}