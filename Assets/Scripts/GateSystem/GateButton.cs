using UnityEngine;
using DG.Tweening;

namespace Iterations.Mechanics
{
    [RequireComponent(typeof(Collider2D))]
    public class GateButton : MonoBehaviour
    {
        [SerializeField] private LayerMask activatorLayers;
        [SerializeField] private Gate[] targetGates;
        [SerializeField] private Transform buttonVisual;
        [SerializeField] private SpriteRenderer buttonSpriteRenderer;

        [SerializeField] private float pressedScaleY = 0.3f;
        [SerializeField] private float animationDuration = 0.2f;
        [SerializeField] private ParticleSystem pressParticles;

        [Header("Audio Setup")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pressSound;
        [SerializeField] private AudioClip releaseSound;

        private int _occupantCount;
        private Vector3 _originalScale;

        private void Start()
        {
            if (buttonVisual != null)
                _originalScale = buttonVisual.localScale;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsValid(other)) return;

            _occupantCount++;

            if (_occupantCount == 1)
            {
                SetGates(open: true);
                AnimatePress();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsValid(other)) return;

            _occupantCount = Mathf.Max(0, _occupantCount - 1);

            if (_occupantCount == 0)
            {
                SetGates(open: false);
                AnimateRelease();
            }
        }

        private void SetGates(bool open)
        {
            foreach (var gate in targetGates)
            {
                if (gate == null) continue;
                if (open) gate.Open();
                else gate.Close();
            }
        }

        private bool IsValid(Collider2D other)
        {
            return (activatorLayers.value & (1 << other.gameObject.layer)) != 0;
        }

        #region Game Feel Animations

        private void AnimatePress()
        {
            if (buttonVisual != null)
            {
                buttonVisual.DOKill();
                buttonVisual.DOScaleY(_originalScale.y * pressedScaleY, animationDuration)
                            .SetEase(Ease.OutBack);
            }

            if (pressParticles != null)
            {
                pressParticles.Play();
            }

            if (audioSource != null && pressSound != null)
            {
                audioSource.PlayOneShot(pressSound);
            }
        }

        private void AnimateRelease()
        {
            if (buttonVisual != null)
            {
                buttonVisual.DOKill();
                buttonVisual.DOScaleY(_originalScale.y, animationDuration * 2f)
                            .SetEase(Ease.OutElastic);
            }

            if (audioSource != null && releaseSound != null)
            {
                audioSource.PlayOneShot(releaseSound);
            }
        }

        #endregion
    }
}