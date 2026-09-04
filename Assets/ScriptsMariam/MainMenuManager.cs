using UnityEngine;
using UnityEngine.SceneManagement;

namespace Iterations.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject soloPanel;
        [SerializeField] private GameObject multiplayerPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject controlsPanel;

        private GameObject[] _allPanels;

        private void Awake()
        {
            _allPanels = new GameObject[]
            {
                mainPanel,
                creditsPanel,
                soloPanel,
                multiplayerPanel,
                settingsPanel,
                controlsPanel
            };

            ShowPanel(mainPanel);
        }

        public void OnCreditsPressed()
        {
            ShowPanel(creditsPanel);
        }

        public void OnSoloPressed()
        {
            ShowPanel(soloPanel);
        }

        public void OnMultiplayerPressed()
        {
            ShowPanel(multiplayerPanel);
        }

        public void OnSettingsPressed()
        {
            ShowPanel(settingsPanel);
        }

        public void OnControlsPressed()
        {
            ShowPanel(controlsPanel);
        }

        public void OnBackPressed()
        {
            ShowPanel(mainPanel);
        }

        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void LoadSceneByName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("No scene name set on this button yet.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private void ShowPanel(GameObject panelToShow)
        {
            foreach (var panel in _allPanels)
            {
                if (panel == null) continue;
                panel.SetActive(panel == panelToShow);
            }
        }
    }
}