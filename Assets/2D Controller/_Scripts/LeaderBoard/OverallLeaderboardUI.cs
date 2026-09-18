using Iterations.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OverallLeaderboardUI : MonoBehaviour
{
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TMP_Text yourScoreText;

    private void Awake()
    {
        leaderboardPanel.SetActive(false);

        openButton.onClick.AddListener(Show);
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void Show()
    {
        leaderboardPanel.SetActive(true);
        Refresh();
    }

    private void Hide() => leaderboardPanel.SetActive(false);

    private void Refresh()
    {
        var sm = ScoreManager.Instance; // adjust to however you access it
        yourScoreText.text =
        $"Your total: {sm.FormatTime(sm.GetOverallScore())} | " +
        $"Retries: {sm.GetOverallRetries()} " +
        $"({sm.GetCompletedLevelCount()}/{sm.TotalLevels} levels)";

        // TODO: fetch and populate leaderboard entries here
    }
}