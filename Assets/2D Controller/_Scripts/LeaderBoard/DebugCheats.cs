using UnityEngine;
using UnityEngine.SceneManagement;
using Iterations.Events;

namespace Iterations.Core
{
    // Compiled only in the Editor and development builds, so it never ships.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class DebugCheats : MonoBehaviour
    {
        [Tooltip("Assign the SAME onWinTriggered asset that GameManager uses.")]
        [SerializeField] private VoidEventChannelSO onWinTriggered;

        [SerializeField] private float fakeBestTime = 30f;
        [SerializeField] private bool showOnScreenButtons = true;

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F1)) WinCurrentLevel();
            if (Input.GetKeyDown(KeyCode.F2)) FillOtherLevelsThenWin();
#endif
        }

        private void OnGUI()
        {
            if (!showOnScreenButtons) return;

            if (GUI.Button(new Rect(10, 10, 220, 30), "Win Level (F1)"))
                WinCurrentLevel();

            if (GUI.Button(new Rect(10, 45, 220, 30), "Fake other levels + Win (F2)"))
                FillOtherLevelsThenWin();
        }

        [ContextMenu("Win Current Level")]
        public void WinCurrentLevel()
        {
            if (onWinTriggered == null)
            {
                Debug.LogError("[DebugCheats] onWinTriggered is not assigned.");
                return;
            }

            Debug.Log("[DebugCheats] Raising win event.");
            onWinTriggered.RaiseEvent();
        }

        // Writes fake best times for every level EXCEPT the current one
        // (existing real times are kept), then wins the current level.
        // After this win, all 12 levels count as completed, so the
        // overall leaderboard submission should fire.
        [ContextMenu("Fake Other Levels + Win")]
        public void FillOtherLevelsThenWin()
        {
            var sm = ScoreManager.Instance;
            if (sm == null)
            {
                Debug.LogError("[DebugCheats] ScoreManager not found.");
                return;
            }

            string current = SceneManager.GetActiveScene().name;

            for (int i = 1; i <= sm.TotalLevels; i++)
            {
                string level = "Level_" + i;
                if (level == current) continue;

                string timeKey = "BestTime_" + level;
                string retriesKey = "BestRetries_" + level;

                if (!PlayerPrefs.HasKey(timeKey))
                {
                    PlayerPrefs.SetFloat(timeKey, fakeBestTime);
                    PlayerPrefs.SetInt(retriesKey, 0);
                }
            }

            PlayerPrefs.Save();
            Debug.Log($"[DebugCheats] Faked other levels. Completed: {sm.GetCompletedLevelCount()}/{sm.TotalLevels}");

            WinCurrentLevel();
        }
    }
#endif
}