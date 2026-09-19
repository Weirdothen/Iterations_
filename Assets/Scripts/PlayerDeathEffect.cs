using DG.Tweening;
using Iterations.Events;
using UnityEngine;

namespace Iterations.Player
{
    public class PlayerDeathEffect : MonoBehaviour
    {
        public GameObject dotPrefab;

        public int dotsCount = 30;
        public float explosionRadius = 3f;
        public float explosionDuration = 0.8f;
        [SerializeField] private VoidEventChannelSO onLoseTriggeredListener;

        private void Start()
        {
            if (onLoseTriggeredListener != null)
            {
                onLoseTriggeredListener.OnEventRaised += () => TriggerExplosion(transform.position);
            }
        }
        public void TriggerExplosion(Vector3 explosionPosition)
        {
            for (int i = 0; i < dotsCount; i++)
            {
                GameObject dot = Instantiate(dotPrefab, explosionPosition, Quaternion.identity);
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                float randomDistance = Random.Range(explosionRadius * 0.5f, explosionRadius);
                Vector3 targetPosition = explosionPosition + (Vector3)(randomDirection * randomDistance);
                dot.transform.DOMove(targetPosition, explosionDuration).SetEase(Ease.OutExpo);
                dot.transform.DOScale(Vector3.zero, explosionDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        Destroy(dot);
                    });
                SpriteRenderer sr = dot.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.DOFade(0, explosionDuration).SetEase(Ease.InQuad);
                }
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (onLoseTriggeredListener != null)
            {
                onLoseTriggeredListener.OnEventRaised -= () => TriggerExplosion(transform.position);
            }
        }
    }
}