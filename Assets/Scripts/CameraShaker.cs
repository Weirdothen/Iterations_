using Iterations.Events;
using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    private Camera targetCamera;

    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float magnitude = 0.1f;
    [SerializeField] private VoidEventChannelSO onLoseTriggeredListener;

    private void Start()
    {
        if (onLoseTriggeredListener != null)
        {
            onLoseTriggeredListener.OnEventRaised += Shake;
        }
        targetCamera = Camera.main;
    }

    public void Shake()
    {
        if (targetCamera != null)
        {
            StartCoroutine(ShakeRoutine(targetCamera, duration, magnitude));
        }
        else
        {
            Debug.LogWarning("No camera has been specified for the shake effect!");
        }
    }

    private IEnumerator ShakeRoutine(Camera cam, float duration, float magnitude)
    {
        Vector3 originalPosition = cam.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cam.transform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.transform.localPosition = originalPosition;
    }
}