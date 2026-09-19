using UnityEngine;
using DG.Tweening;

public class WinPanelTween : MonoBehaviour
{
    [SerializeField] private float duration = 0.25f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void Start()
    {
        PlayTween();
    }

    public void PlayTween()
    {
        transform.DOKill();

        transform.localScale = Vector3.zero;

        transform.DOScale(originalScale, duration)
            .SetEase(Ease.OutBack);
    }
}