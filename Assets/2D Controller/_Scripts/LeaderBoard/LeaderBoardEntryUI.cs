using LightSide;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Services.Leaderboards.Models;
using UnityEngine;


namespace Iterations.Core
{
    public class LeaderBoardEntryUI : MonoBehaviour
    {
        [SerializeField] private UniText rankText;
        [SerializeField] private UniText nameText;
        [SerializeField] private UniText attemptsText;
        [SerializeField] private UniText timeText;

        public void Setup(LeaderboardEntry entry)
        {
            if (rankText == null || nameText == null || attemptsText == null || timeText == null)
            {
                Debug.LogError(
                    "[LeaderBoardEntryUI] One or more text fields are not assigned " +
                    "on the entry PREFAB itself. Select the prefab asset (not the " +
                    "instance in the scene) and drag its child text objects into these fields."
                );
                return;
            }

            rankText.Text = (entry.Rank + 1).ToString();
            nameText.Text = StripNameSuffix(entry.PlayerName);
            attemptsText.Text = GetAttempts(entry);
            timeText.Text = FormatTime(entry.Score);
        }

        private string GetAttempts(LeaderboardEntry entry)
        {
            if (string.IsNullOrEmpty(entry.Metadata))
                return "0";

            try
            {
                ScoreMetadata metadata =
                    JsonUtility.FromJson<ScoreMetadata>(entry.Metadata);

                return metadata.retries.ToString();
            }
            catch
            {
                return "0";
            }
        }

        private string FormatTime(double milliseconds)
        {
            double seconds = milliseconds / 1000.0;

            int minutes = Mathf.FloorToInt((float)seconds / 60f);

            int remainingSeconds =
                Mathf.FloorToInt((float)seconds % 60f);

            int millisecondsPart =
                Mathf.FloorToInt((float)(milliseconds % 1000));

            return $"{minutes:00}:{remainingSeconds:00}:{millisecondsPart:000}";
        }
        private static string StripNameSuffix(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
                return "Unknown";

            // Removes a trailing "#" followed by digits, e.g. "Sam#123456" -> "Sam"
            return Regex.Replace(playerName, @"#\d+$", "");
        }

        [System.Serializable]
        private class ScoreMetadata
        {
            public int retries;
        }
    }
}