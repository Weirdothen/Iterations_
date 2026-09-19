using UnityEngine;
using DG.Tweening;

public class MainMenuTween : MonoBehaviour
{
    [SerializeField] private RectTransform logo;
    [SerializeField] private RectTransform soloButton;
    [SerializeField] private RectTransform multiplayerButton;
    [SerializeField] private RectTransform creditsButton;
    [SerializeField] private RectTransform settingsButton;
    [SerializeField] private RectTransform controlsButton;
    [SerializeField] private RectTransform LeaderboardButton;
    [SerializeField] private RectTransform quitButton;

    private void Start()
    {
        PlayIntro();
    }

    private void PlayIntro()
    {
        // Save their normal positions
        Vector2 logoPos = logo.anchoredPosition;
        Vector2 soloPos = soloButton.anchoredPosition;
        Vector2 multiplayerPos = multiplayerButton.anchoredPosition;

        // Move them away first
        logo.anchoredPosition = logoPos + Vector2.up * 200f;
        soloButton.anchoredPosition = soloPos + Vector2.down * 100f;
        multiplayerButton.anchoredPosition = multiplayerPos + Vector2.down * 100f;

        // Animate logo into position
        logo.DOAnchorPos(logoPos, 0.7f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // Start logo idle after intro finishes
                logo.DOAnchorPosY(logoPos.y + 6f, 1.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            });

        // Animate main buttons
        soloButton.DOAnchorPos(soloPos, 0.6f)
            .SetDelay(0.3f)
            .SetEase(Ease.OutBack);

        multiplayerButton.DOAnchorPos(multiplayerPos, 0.6f)
            .SetDelay(0.4f)
            .SetEase(Ease.OutBack);

        // Bottom buttons
        Vector2 creditsPos = creditsButton.anchoredPosition;
        Vector2 settingsPos = settingsButton.anchoredPosition;
        Vector2 controlsPos = controlsButton.anchoredPosition;
        Vector2 quitPos = quitButton.anchoredPosition;
        Vector2 leaderBoardPos = LeaderboardButton.anchoredPosition;

        creditsButton.anchoredPosition = creditsPos + Vector2.down * 80f;
        settingsButton.anchoredPosition = settingsPos + Vector2.down * 80f;
        controlsButton.anchoredPosition = controlsPos + Vector2.down * 80f;
        quitButton.anchoredPosition = quitPos + Vector2.down * 80f;
        LeaderboardButton.anchoredPosition = leaderBoardPos + Vector2.down * 80f;

        creditsButton.DOAnchorPos(creditsPos, 0.5f)
            .SetDelay(0.6f)
            .SetEase(Ease.OutBack);

        settingsButton.DOAnchorPos(settingsPos, 0.5f)
            .SetDelay(0.7f)
            .SetEase(Ease.OutBack);

        controlsButton.DOAnchorPos(controlsPos, 0.5f)
            .SetDelay(0.8f)
            .SetEase(Ease.OutBack);

        quitButton.DOAnchorPos(quitPos, 0.5f)
            .SetDelay(0.9f)
            .SetEase(Ease.OutBack);
        LeaderboardButton.DOAnchorPos(leaderBoardPos, 0.5f)
            .SetDelay(0.9f)
            .SetEase(Ease.OutBack);
    }
}