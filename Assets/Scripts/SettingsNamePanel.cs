using UnityEngine;
using TMPro;

namespace Iterations.Core
{
    
    public class SettingsNamePanel : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TMP_InputField settingsNameInput;
        [SerializeField] private TMP_Text settingsErrorText;

        public void OpenPanel()
        {
            if (settingsNameInput != null && UGSManager.Instance != null)
                settingsNameInput.text = UGSManager.Instance.GetSavedName();

            if (settingsErrorText != null)
                settingsErrorText.text = "";

            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        public async void OnConfirmPressed()
        {
            if (UGSManager.Instance == null)
            {
                Debug.LogWarning("OnConfirmPressed: UGSManager.Instance is null.");
                return;
            }

            if (settingsNameInput == null)
                return;

            await UGSManager.Instance.TrySetPlayerName(settingsNameInput.text, settingsErrorText);
        }
    }
}