using UnityEngine;
using TMPro;

/// <summary>
/// Attach to your Main Menu Canvas.
/// Automatically shows a popup if the player arrived here due to a
/// connection failure or disconnection.
/// </summary>
public class ConnectionFailedPopupUI : MonoBehaviour
{
    [Header("Popup References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text messageText;

    private void Start()
    {
        if (ConnectionFailedData.HasMessage)
        {
            if (popupPanel != null)  popupPanel.SetActive(true);
            if (messageText != null) messageText.text = ConnectionFailedData.Message;

            // Clear so it doesn't show next time the menu is visited normally
            ConnectionFailedData.HasMessage = false;
            ConnectionFailedData.Message   = "";
        }
        else
        {
            if (popupPanel != null) popupPanel.SetActive(false);
        }
    }

    /// <summary>Hook this to your popup's OK / Close button.</summary>
    public void ClosePopup()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }
}
