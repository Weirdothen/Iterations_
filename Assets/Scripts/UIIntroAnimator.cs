using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Put this on a panel / container. Its buttons (or any UI elements) animate in
/// one after another: slide + pop scale + fade. Replays every time the panel is enabled.
/// Requires DOTween.
/// </summary>
[DisallowMultipleComponent]
public class UIIntroAnimator : MonoBehaviour
{
    public enum SlideDirection { FromBottom, FromTop, FromLeft, FromRight, None }

    [Header("Targets")]
    [Tooltip("Leave EMPTY to animate every active direct child of this object, in order. " +
             "Or drag specific buttons here in the order you want them to appear.")]
    [SerializeField] private List<RectTransform> targets = new List<RectTransform>();

    [Header("Animation")]
    [SerializeField] private SlideDirection direction = SlideDirection.FromBottom;
    [Tooltip("How far (in pixels) each element starts away from its final position.")]
    [SerializeField] private float slideDistance = 100f;
    [Tooltip("Every second element comes from the opposite side (nice with Left / Right).")]
    [SerializeField] private bool alternateSides = false;
    [SerializeField] private float duration = 0.55f;
    [SerializeField] private Ease ease = Ease.OutBack;
    [Tooltip("Fade from transparent to visible while sliding in.")]
    [SerializeField] private bool fade = true;
    [Tooltip("Scale each element starts from (1 = no scale pop, 0.8 = grows from 80%).")]
    [SerializeField] private float startScale = 0.85f;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.2f;
    [Tooltip("Seconds between one element and the next.")]
    [SerializeField] private float delayBetweenItems = 0.1f;
    [SerializeField] private bool playOnEnable = true;
    [Tooltip("Keeps animating even when Time.timeScale is 0 (pause menus).")]
    [SerializeField] private bool useUnscaledTime = true;

    // Remembers each element's final state so it can always be restored
    private class Item
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector2 endPosition;
        public Vector3 endScale;
    }

    private readonly List<Item> items = new List<Item>();

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    private void OnDisable()
    {
        // Stop everything and leave all elements fully visible in their final place,
        // so nothing can ever stay hidden or offset.
        DOTween.Kill(this);
        RestoreAll();
    }

    // You can also call this from code or a button's OnClick
    public void Play()
    {
        DOTween.Kill(this);
        RestoreAll();
        items.Clear();

        List<RectTransform> list = GetTargets();
        if (list.Count == 0) return;

        // Make sure any layout is up to date so we capture the FINAL positions
        Canvas.ForceUpdateCanvases();

        Vector2 baseOffset = GetStartOffset();

        for (int i = 0; i < list.Count; i++)
        {
            RectTransform rt = list[i];

            Item item = new Item
            {
                rect = rt,
                endPosition = rt.anchoredPosition,
                endScale = rt.localScale
            };

            if (fade)
            {
                item.group = rt.GetComponent<CanvasGroup>();
                if (item.group == null) item.group = rt.gameObject.AddComponent<CanvasGroup>();
            }

            items.Add(item);

            // Start state
            Vector2 offset = (alternateSides && i % 2 == 1) ? -baseOffset : baseOffset;
            rt.anchoredPosition = item.endPosition + offset;
            rt.localScale = item.endScale * startScale;
            if (item.group != null) item.group.alpha = 0f;

            float delay = startDelay + i * delayBetweenItems;

            // Slide
            if (direction != SlideDirection.None)
            {
                rt.DOAnchorPos(item.endPosition, duration)
                    .SetDelay(delay)
                    .SetEase(ease)
                    .SetUpdate(useUnscaledTime)
                    .SetTarget(this);
            }

            // Scale pop
            if (!Mathf.Approximately(startScale, 1f))
            {
                rt.DOScale(item.endScale, duration)
                    .SetDelay(delay)
                    .SetEase(ease)
                    .SetUpdate(useUnscaledTime)
                    .SetTarget(this);
            }

            // Fade (a bit faster than the slide so it feels snappy)
            if (item.group != null)
            {
                item.group.DOFade(1f, duration * 0.6f)
                    .SetDelay(delay)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(useUnscaledTime)
                    .SetTarget(this);
            }
        }
    }

    private List<RectTransform> GetTargets()
    {
        List<RectTransform> result = new List<RectTransform>();

        if (targets != null && targets.Count > 0)
        {
            foreach (RectTransform rt in targets)
            {
                if (rt != null && rt.gameObject.activeInHierarchy) result.Add(rt);
            }
        }
        else
        {
            foreach (Transform child in transform)
            {
                RectTransform rt = child as RectTransform;
                if (rt != null && child.gameObject.activeSelf) result.Add(rt);
            }
        }

        return result;
    }

    private Vector2 GetStartOffset()
    {
        switch (direction)
        {
            case SlideDirection.FromBottom: return Vector2.down * slideDistance;
            case SlideDirection.FromTop: return Vector2.up * slideDistance;
            case SlideDirection.FromLeft: return Vector2.left * slideDistance;
            case SlideDirection.FromRight: return Vector2.right * slideDistance;
            default: return Vector2.zero;
        }
    }

    private void RestoreAll()
    {
        foreach (Item item in items)
        {
            if (item.rect == null) continue;

            item.rect.anchoredPosition = item.endPosition;
            item.rect.localScale = item.endScale;
            if (item.group != null) item.group.alpha = 1f;
        }
    }
}