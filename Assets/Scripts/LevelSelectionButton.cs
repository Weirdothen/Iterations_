using UnityEngine;
using UnityEngine.UI;
using Iterations.Core;

namespace Iterations.UI
{
    [RequireComponent(typeof(Button))]
    public class LevelSelectionButton : MonoBehaviour
    {
        [SerializeField] private string levelSceneName;
        [SerializeField] private bool isUnlockedByDefault = false;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void Start()
        {
            CheckUnlockStatus();
            button.onClick.AddListener(OnLevelButtonClicked);
        }

        private void CheckUnlockStatus()
        {
            if (isUnlockedByDefault)
            {
                button.interactable = true;
                return;
            }
            int isUnlocked = PlayerPrefs.GetInt(levelSceneName, 0);

            if (isUnlocked == 1)
            {
                button.interactable = true;
            }
            else
            {
                button.interactable = false;
            }
        }

        private void OnLevelButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadLevelFromMenu(levelSceneName);
            }
            else
            {
                Debug.LogWarning("GameManager not found!");
            }
        }
    }
}