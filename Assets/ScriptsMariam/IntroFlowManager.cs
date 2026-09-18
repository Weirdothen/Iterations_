using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroFlowManager : MonoBehaviour
{
    private enum FlowState
    {
        PlayingIntro,
        PlayingCutscene
    }

    [Header("Video Players (one per clip)")]
    [SerializeField] private VideoPlayer introVideoPlayer;
    [SerializeField] private VideoPlayer cutsceneVideoPlayer;

    [Header("Skip Prompt (only shown on non-first launches, during the cutscene)")]
    [SerializeField] private GameObject skipPromptUI;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;

    [Header("Scene Names")]
    [SerializeField] private string tutorialSceneName = "TutorialScene";
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("PlayerPrefs")]
    [SerializeField] private string introWatchedKey = "IntroWatched";

    [Header("Startup Delay")]
    [SerializeField] private float startDelay = 0.4f;

    private FlowState currentState;
    private bool isFirstLaunch;
    private bool hasTransitioned;

    private void Awake()
    {
        isFirstLaunch = PlayerPrefs.GetInt(introWatchedKey, 0) == 0;
    }

    private void Start()
    {
        currentState = FlowState.PlayingIntro;

        if (skipPromptUI != null)
        {
            skipPromptUI.SetActive(false);
        }

        cutsceneVideoPlayer.gameObject.SetActive(false);

        introVideoPlayer.loopPointReached += OnIntroFinished;
        cutsceneVideoPlayer.loopPointReached += OnCutsceneFinished;

        StartCoroutine(PlayIntroAfterDelay());
    }

    private IEnumerator PlayIntroAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        introVideoPlayer.Play();
    }

    private void Update()
    {
        if (isFirstLaunch)
        {
            return;
        }

        if (currentState != FlowState.PlayingCutscene || hasTransitioned)
        {
            return;
        }

        if (Input.GetKeyDown(skipKey))
        {
            OnCutsceneFinished(cutsceneVideoPlayer);
        }
    }

    private void OnIntroFinished(VideoPlayer vp)
    {
        currentState = FlowState.PlayingCutscene;

        introVideoPlayer.gameObject.SetActive(false);
        cutsceneVideoPlayer.gameObject.SetActive(true);

        if (skipPromptUI != null)
        {
            skipPromptUI.SetActive(!isFirstLaunch);
        }

        cutsceneVideoPlayer.Play();
    }

    private void OnCutsceneFinished(VideoPlayer vp)
    {
        if (hasTransitioned)
        {
            return;
        }
        hasTransitioned = true;

        if (isFirstLaunch)
        {
            SaveIntroWatchedFlag();
            SceneManager.LoadScene(tutorialSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void SaveIntroWatchedFlag()
    {
        PlayerPrefs.SetInt(introWatchedKey, 1);
        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}