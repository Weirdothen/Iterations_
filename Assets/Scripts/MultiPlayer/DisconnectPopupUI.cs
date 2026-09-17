using UnityEngine;
using TMPro;

public class DisconnectPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text popupMessageText;

    private void Start()
    {
        // Check if we arrived here because of a disconnect
        if (DisconnectReasonData.HasDisconnectedMessage)
        {
            if (popupPanel != null) popupPanel.SetActive(true);
            if (popupMessageText != null) popupMessageText.text = DisconnectReasonData.DisconnectMessage;

            // Reset it so it doesn't show again next time they visit the main menu normally
            DisconnectReasonData.HasDisconnectedMessage = false;
        }
        else
        {
            // Ensure it's hidden by default
            if (popupPanel != null) popupPanel.SetActive(false);
        }
    }

    // Call this from an "OK" button on the popup
    public void ClosePopup()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }
}
