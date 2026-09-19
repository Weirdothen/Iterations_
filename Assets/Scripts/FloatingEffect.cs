using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drop this on ANY UI Image (or any object) to make it float.
/// Combines: drifting movement + slight tilt + soft breathing scale.
/// Optional: grows a little when the mouse hovers over it.
/// Requires DOTween.
/// </summary>
[DisallowMultipleComponent]
public class FloatingEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Float Movement")]
    [Tooltip("How far it drifts on each axis (pixels for UI). Set an axis to 0 to disable it.")]
    [SerializeField] private Vector2 amplitude = new Vector2(6f, 14f);
    [Tooltip("Seconds for one drift on X. Using different X/Y times makes the motion feel organic.")]
    [SerializeField] private float durationX = 2.4f;
    [Tooltip("Seconds for one drift on Y.")]
    [SerializeField] private float durationY = 3.1f;
    [SerializeField] private Ease moveEase = Ease.InOutSine;

    [Header("Tilt")]
    [Tooltip("Degrees it rocks left and right. 0 = no tilt.")]
    [SerializeField] private float tiltAngle = 2f;
    [SerializeField] private float tiltDuration = 3.6f;

    [Header("Breathing (soft scale pulse)")]
    [Tooltip("0.03 = grows up to 3%. 0 = no breathing.")]
    [SerializeField] private float breathAmount = 0.03f;
    [SerializeField] private float breathDuration = 2.8f;

    [Header("Timing")]
    [Tooltip("Starts each object at a random point so several floating objects don't move in sync.")]
    [SerializeField] private bool randomizeStart = true;
    [Tooltip("Keeps floating even when Time.timeScale is 0 (pause menus).")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Hover Reaction (needs Raycast Target ON on the Image)")]
    [SerializeField] private bool reactToHover = true;
    [Tooltip("1.08 = grows 8% while the mouse is over it.")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverDuration = 0.25f;

    private RectTransform rect;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;

    private Vector2 offset;
    private float tilt;
    private float breath;
    private float hoverMultiplier = 1f;
    private Tween hoverTween;

    private void Awake()
    {
        rect = transform as RectTransform;
    }

    private void OnEnable()
    {
        CaptureBase();
        StartFloating();
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        hoverTween = null;
        RestoreBase();
    }

    // ─── Setup ────────────────────────────────────────────────────────────────

    private void CaptureBase()
    {
        basePosition = rect != null ? rect.anchoredPosition3D : transform.localPosition;
        baseRotation = transform.localRotation;
        baseScale = transform.localScale;
    }

    private void RestoreBase()
    {
        if (rect != null) rect.anchoredPosition3D = basePosition;
        else transform.localPosition = basePosition;

        transform.localRotation = baseRotation;
        transform.localScale = baseScale;
    }

    private void StartFloating()
    {
        offset = Vector2.zero;
        tilt = 0f;
        breath = 0f;
        hoverMultiplier = 1f;

        // Drift on X (goes from -amplitude to +amplitude and back, centered on the start position)
        if (amplitude.x > 0f)
        {
            offset.x = -amplitude.x;
            StartLoop(() => offset.x, v => offset.x = v, amplitude.x, durationX, moveEase);
        }

        // Drift on Y
        if (amplitude.y > 0f)
        {
            offset.y = -amplitude.y;
            StartLoop(() => offset.y, v => offset.y = v, amplitude.y, durationY, moveEase);
        }

        // Tilt
        if (tiltAngle > 0f)
        {
            tilt = -tiltAngle;
            StartLoop(() => tilt, v => tilt = v, tiltAngle, tiltDuration, Ease.InOutSine);
        }

        // Breathing
        if (breathAmount > 0f)
        {
            breath = 0f;
            StartLoop(() => breath, v => breath = v, breathAmount, breathDuration, Ease.InOutSine);
        }

        ApplyAll();
    }

    // One endless back-and-forth tween on a single value
    private void StartLoop(DOGetter<float> getter, DOSetter<float> setter, float target, float time, Ease ease)
    {
        Tween tween = DOTween.To(getter, v => { setter(v); ApplyAll(); }, target, time)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(useUnscaledTime)
            .SetTarget(this);

        // Jump to a random point in the loop so objects don't float in sync
        if (randomizeStart)
        {
            tween.Goto(Random.Range(0f, time * 2f), true);
        }
    }

    // Combines every effect into the final position / rotation / scale
    private void ApplyAll()
    {
        Vector3 pos = basePosition + new Vector3(offset.x, offset.y, 0f);

        if (rect != null) rect.anchoredPosition3D = pos;
        else transform.localPosition = pos;

        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, tilt);
        transform.localScale = baseScale * ((1f + breath) * hoverMultiplier);
    }

    // ─── Hover ────────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!reactToHover) return;
        TweenHover(hoverScale, Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!reactToHover) return;
        TweenHover(1f, Ease.OutQuad);
    }

    private void TweenHover(float target, Ease ease)
    {
        if (hoverTween != null) hoverTween.Kill();

        hoverTween = DOTween.To(() => hoverMultiplier, v => { hoverMultiplier = v; ApplyAll(); }, target, hoverDuration)
            .SetEase(ease)
            .SetUpdate(useUnscaledTime)
            .SetTarget(this);
    }
}