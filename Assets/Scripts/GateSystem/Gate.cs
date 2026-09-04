using System.Collections;
using UnityEngine;
using Iterations.Events;

namespace Iterations.Mechanics
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Gate : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO onGateShouldOpen;
        [SerializeField] private VoidEventChannelSO onGateShouldClose;

        [SerializeField] private float slideDistance = 2f;
        [SerializeField] private float slideDuration = 0.4f;
        [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Collider2D _collider;
        private SpriteRenderer _renderer;
        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private Coroutine _slideRoutine;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<SpriteRenderer>();

            _closedPosition = transform.position;
            _openPosition = _closedPosition + new Vector3(0, slideDistance, 0);
        }

        private void OnEnable()
        {
            if (onGateShouldOpen != null) onGateShouldOpen.OnEventRaised += Open;
            if (onGateShouldClose != null) onGateShouldClose.OnEventRaised += Close;
        }

        private void OnDisable()
        {
            if (onGateShouldOpen != null) onGateShouldOpen.OnEventRaised -= Open;
            if (onGateShouldClose != null) onGateShouldClose.OnEventRaised -= Close;
        }

        private void Open()
        {
            _collider.enabled = false;
            StartSlide(_openPosition, fadeOut: true);
        }

        private void Close()
        {
            StartSlide(_closedPosition, fadeOut: false);
        }

        private void StartSlide(Vector3 target, bool fadeOut)
        {
            if (_slideRoutine != null) StopCoroutine(_slideRoutine);
            _slideRoutine = StartCoroutine(SlideRoutine(target, fadeOut));
        }

        private IEnumerator SlideRoutine(Vector3 target, bool fadeOut)
        {
            Vector3 start = transform.position;
            Color startColor = _renderer.color;
            Color targetColor = startColor;
            targetColor.a = fadeOut ? 0f : 1f;

            float t = 0f;
            while (t < slideDuration)
            {
                t += Time.deltaTime;
                float eval = ease.Evaluate(Mathf.Clamp01(t / slideDuration));
                transform.position = Vector3.Lerp(start, target, eval);
                _renderer.color = Color.Lerp(startColor, targetColor, eval);
                yield return null;
            }

            transform.position = target;
            _renderer.color = targetColor;

            if (!fadeOut)
                _collider.enabled = true;
        }
    }
}