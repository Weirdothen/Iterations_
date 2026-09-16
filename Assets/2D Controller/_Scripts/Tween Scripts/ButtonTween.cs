using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.DOKill();

        rectTransform.DOScale(originalScale * 1.05f, 0.15f)
            .SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.DOKill();

        rectTransform.DOScale(originalScale, 0.15f)
            .SetEase(Ease.OutBack);
    }

}