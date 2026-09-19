using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;

    [Header("Music Settings")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private string musicParameter = "MusicVolume";

    [Header("SFX Settings")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private string sfxParameter = "SFXVolume";

    private void Start()
    {
        if (PlayerPrefs.HasKey(musicParameter))
            musicSlider.value = PlayerPrefs.GetFloat(musicParameter);
        else
            musicSlider.value = 0.2f;

        if (PlayerPrefs.HasKey(sfxParameter))
            sfxSlider.value = PlayerPrefs.GetFloat(sfxParameter);
        else
            sfxSlider.value = 1f;
        SetMusicVolume();
        SetSFXVolume();
    }

    public void SetMusicVolume()
    {
        float volume = musicSlider.value;
        myMixer.SetFloat(musicParameter, Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat(musicParameter, volume);
    }

    public void SetSFXVolume()
    {
        float volume = sfxSlider.value;
        myMixer.SetFloat(sfxParameter, Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat(sfxParameter, volume);
    }
}