using UnityEngine;
using UnityEngine.UI;

public class CloneTimerUI : MonoBehaviour
{
    [SerializeField] private Image timerImage;
    [SerializeField] private float firstCloneTime = 5f;
    [SerializeField] private float cloneSpawningTime = 5f;

    private float currentTimer;
    private float currentDuration;

    private void Start()
    {
        currentDuration = firstCloneTime;
        currentTimer = currentDuration;
    }

    private void Update()
    {
        if (timerImage == null) return;
        currentTimer -= Time.deltaTime;
        timerImage.fillAmount = currentTimer / currentDuration;
        if (currentTimer <= 0)
        {
            currentDuration = cloneSpawningTime;
            currentTimer = currentDuration;
            timerImage.fillAmount = 1f;
        }
    }
}