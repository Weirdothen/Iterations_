using UnityEngine;

public class InAllScene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       DontDestroyOnLoad(gameObject);
        GetComponent<AudioSource>().volume =  PlayerPrefs.GetFloat("MusicVolume", .2f);
    }
}
