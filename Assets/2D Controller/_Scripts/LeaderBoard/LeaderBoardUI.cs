using System;
using TMPro;
using UnityEngine;
using Unity.Services.Leaderboards.Models;
using LightSide;

namespace Iterations.Core
{
    public class LeaderboardUI : MonoBehaviour
    {
        public static LeaderboardUI Instance { get; private set; }

        [Header("Main UI")]
        [SerializeField] private GameObject leaderboardPanel;

        [Header("Entries")]
        [SerializeField] private Transform entriesContainer;
        [SerializeField] private GameObject entryPrefab;

        private int requestId = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            ShowLeaderboard();
        }

        public async void ShowLeaderboard()
        {
            Debug.Log("[LeaderboardUI] ShowLeaderboard CALLED");

            if (leaderboardPanel == null || entriesContainer == null || entryPrefab == null)
            {
                Debug.LogError(
                    "[LeaderboardUI] Missing Inspector references " +
                    "(leaderboardPanel / entriesContainer / entryPrefab)."
                );
                return;
            }

            // Mark this as the newest request; any older call still
            // awaiting the network will check this and back off.
            int thisRequest = ++requestId;

            leaderboardPanel.SetActive(true);

            ClearEntries();

            if (OnlineLeaderboardManager.Instance == null)
            {
                Debug.LogError(
                    "[LeaderboardUI] OnlineLeaderboardManager not found."
                );
                return;
            }

            LeaderboardScoresPage leaderboard =
                await OnlineLeaderboardManager.Instance
                    .GetCurrentLevelLeaderboard();

            // If another call started after this one, drop these
            // results — they're stale, the newer call owns the container.
            if (thisRequest != requestId)
            {
                Debug.Log("[LeaderboardUI] Stale request, discarding results.");
                return;
            }

            if (leaderboard == null)
            {
                Debug.LogWarning(
                    "[LeaderboardUI] Could not load leaderboard."
                );
                return;
            }

            Debug.Log(
                $"[LeaderboardUI] Leaderboard loaded! Entries: {leaderboard.Results.Count}"
            );

            // Clear again right before populating, in case anything
            // slipped in between the first clear and now.
            ClearEntries();

            foreach (LeaderboardEntry entry in leaderboard.Results)
            {
                Debug.Log(
                    $"[LeaderboardUI] Creating entry: {entry.Rank} - {entry.PlayerName}"
                );

                CreateEntry(entry);
            }
        }

        public void HideLeaderboard()
        {
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);
        }

        private void CreateEntry(LeaderboardEntry entry)
        {
            GameObject newEntry =
                Instantiate(entryPrefab, entriesContainer);

            LeaderBoardEntryUI entryUI =
                newEntry.GetComponent<LeaderBoardEntryUI>();

            if (entryUI == null)
            {
                Debug.LogError(
                    "[LeaderboardUI] Entry prefab is missing LeaderBoardEntryUI component."
                );
                return;
            }

            entryUI.Setup(entry);
        }

        private void ClearEntries()
        {
            foreach (Transform child in entriesContainer)
            {
                Destroy(child.gameObject);
            }
        }

        [Serializable]
        private class ScoreMetadata
        {
            public int retries;
        }
    }
}