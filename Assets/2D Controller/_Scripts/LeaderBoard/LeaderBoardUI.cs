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

        


        private void Awake()
        {
            Instance = this;
        }

        public async void ShowLeaderboard()
        {
            Debug.Log("[LeaderboardUI] ShowLeaderboard CALLED");

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

            foreach (LeaderboardEntry entry in leaderboard.Results)
            {
                Debug.Log(
                    $"[LeaderboardUI] Creating entry: {entry.Rank} - {entry.PlayerName}"
                );

                CreateEntry(entry);
            }
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
                    "[LeaderboardUI] Entry prefab is missing LeaderboardEntryUI."
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