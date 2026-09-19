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
    [Tooltip("If ON, the skip key also works on the very first launch (emergency skip)")]
    [SerializeField] private bool allowSkipOnFirstLaunch = false;

    [Header("Scene Names")]
    [SerializeField] private string tutorialSceneName = "TutorialScene";
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("PlayerPrefs")]
    [SerializeField] private string introWatchedKey = "IntroWatched";

    [Header("Startup Delay")]
    [SerializeField] private float startDelay = 0.4f;

    [Header("Safety Net (prevents getting stuck)")]
    [Tooltip("Max seconds to wait for a video to prepare before skipping it")]
    [SerializeField] private float prepareTimeout = 6f;
    [Tooltip("If a playing video's time doesn't move for this many seconds, treat it as stuck and move on")]
    [SerializeField] private float stallTimeout = 3f;
    [Tooltip("Extra seconds allowed beyond the video's length before forcing it to finish")]
    [SerializeField] private float overrunGrace = 2f;

    private FlowState currentState;
    private bool isFirstLaunch;
    private bool hasTransitioned;

    private VideoPlayer currentPlayer;
    private bool videoDone;
    private bool skipRequested;

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

        introVideoPlayer.loopPointReached += OnVideoEnded;
        cutsceneVideoPlayer.loopPointReached += OnVideoEnded;
        introVideoPlayer.errorReceived += OnVideoError;
        cutsceneVideoPlayer.errorReceived += OnVideoError;

        StartCoroutine(FlowRoutine());
    }

    private void OnDestroy()
    {
        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnVideoEnded;
            introVideoPlayer.errorReceived -= OnVideoError;
        }
        if (cutsceneVideoPlayer != null)
        {
            cutsceneVideoPlayer.loopPointReached -= OnVideoEnded;
            cutsceneVideoPlayer.errorReceived -= OnVideoError;
        }
    }

    private void Update()
    {
        if (hasTransitioned) return;
        if (currentState != FlowState.PlayingCutscene) return;
        if (isFirstLaunch && !allowSkipOnFirstLaunch) return;

        if (Input.GetKeyDown(skipKey))
        {
            skipRequested = true;
        }
    }

    // ─── Main flow ────────────────────────────────────────────────────────────

    private IEnumerator FlowRoutine()
    {
        yield return new WaitForSecondsRealtime(startDelay);

        // 1) Intro
        yield return PlayVideoSafely(introVideoPlayer);

        // 2) Cutscene (switched over outside of any video callback)
        currentState = FlowState.PlayingCutscene;
        introVideoPlayer.gameObject.SetActive(false);
        cutsceneVideoPlayer.gameObject.SetActive(true);

        if (skipPromptUI != null)
        {
            skipPromptUI.SetActive(!isFirstLaunch || allowSkipOnFirstLaunch);
        }

        yield return PlayVideoSafely(cutsceneVideoPlayer);

        // 3) Done - always leave this scene, whatever happened above
        FinishFlow();
    }

    // Plays one video and returns when it ended, failed, stalled, or was skipped.
    // It can never wait forever.
    private IEnumerator PlayVideoSafely(VideoPlayer vp)
    {
        currentPlayer = vp;
        videoDone = false;

        vp.isLooping = false;
        vp.skipOnDrop = true;

        // Prepare first, with a timeout
        vp.Prepare();
        float waited = 0f;
        while (!vp.isPrepared && !videoDone && waited < prepareTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!vp.isPrepared)
        {
            Debug.LogWarning("IntroFlowManager: video failed to prepare in time, skipping it: " + vp.name);
            currentPlayer = null;
            yield break;
        }

        vp.Play();

        float maxDuration = (float)vp.length + overrunGrace;
        float elapsed = 0f;
        float stalledFor = 0f;
        float lastTime = -1f;

        while (!videoDone)
        {
            if (skipRequested && currentState == FlowState.PlayingCutscene)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;

            // Reached the end of the clip but the end event never came
            if (vp.length > 0 && vp.time >= vp.length - 0.05)
            {
                break;
            }

            // Stall detection: the playback time stopped moving
            float t = (float)vp.time;
            if (Mathf.Approximately(t, lastTime))
            {
                stalledFor += Time.unscaledDeltaTime;
            }
            else
            {
                stalledFor = 0f;
                lastTime = t;
            }

            if (stalledFor >= stallTimeout)
            {
                Debug.LogWarning("IntroFlowManager: video stalled, moving on: " + vp.name);
                break;
            }

            if (maxDuration > overrunGrace && elapsed >= maxDuration)
            {
                Debug.LogWarning("IntroFlowManager: video ran longer than its length, moving on: " + vp.name);
                break;
            }

            yield return null;
        }

        vp.Stop();
        currentPlayer = null;
    }

    private void OnVideoEnded(VideoPlayer vp)
    {
        if (vp == currentPlayer) videoDone = true;
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogWarning("IntroFlowManager: video error on " + vp.name + ": " + message);
        if (vp == currentPlayer) videoDone = true;
    }

    private void FinishFlow()
    {
        if (hasTransitioned) return;
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