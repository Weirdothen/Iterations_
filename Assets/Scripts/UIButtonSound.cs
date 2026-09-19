using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private AudioSource audioSource;

    [Header("Audio Clips")]
    public AudioClip[] hoverInClips;
    public AudioClip[] hoverOutClips;
    public AudioClip[] clickClips;

    [Header("Pitch Randomization")]
    [Range(0.5f, 2f)] public float minPitch = 0.9f;
    [Range(0.5f, 2f)] public float maxPitch = 1.1f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Click);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayRandomSound(hoverInClips);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayRandomSound(hoverOutClips);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayRandomSound(clickClips);
    }

    public void Click()
    {
        PlayRandomSound(clickClips);
    }

    private void PlayRandomSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip randomClip = clips[Random.Range(0, clips.Length)];
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(randomClip);
    }
}