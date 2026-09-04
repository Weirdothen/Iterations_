using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene"; 

    private void Start()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        LoadMenu();
    }

    private void Update()
    {
        //press esc r space to skip
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            LoadMenu();
        }
    }

    private void LoadMenu()
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
