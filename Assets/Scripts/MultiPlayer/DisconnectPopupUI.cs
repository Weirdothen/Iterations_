using UnityEngine;
using TMPro;

/// <summary>
/// Attach to your Main Menu Canvas.
/// Shows a popup whenever ConnectionFailedData has a pending message.
/// Works both on scene load (e.g. kicked from game) AND in real-time
/// (e.g. join code rejected while still on the menu).
/// </summary>
public class ConnectionFailedPopupUI : MonoBehaviour
{
    [Header("Popup References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text   messageText;

    private void Start()
    {
        // Check immediately in case we arrived here after being disconnected from a game
        TryShowPendingMessage();
    }

    private void Update()
    {
        // Poll each frame so rejection messages that arrive while on the menu
        // (e.g. join code invalid, room full) are shown without a scene reload.
        if (ConnectionFailedData.HasMessage)
        {
            TryShowPendingMessage();
        }
    }

    private void TryShowPendingMessage()
    {
        if (!ConnectionFailedData.HasMessage) return;

        if (popupPanel != null) popupPanel.SetActive(true);
        if (messageText != null) messageText.text = ConnectionFailedData.Message;

        // Clear so it doesn't show again on the next frame or next visit
        ConnectionFailedData.HasMessage = false;
        ConnectionFailedData.Message    = string.Empty;
    }

    /// <summary>Hook this to your popup's OK / Close button.</summary>
    public void ClosePopup()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }
}
