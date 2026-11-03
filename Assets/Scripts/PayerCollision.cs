using UnityEngine;
using UnityEngine.SceneManagement;
using Iterations.Events;
using Iterations.Core;
using KajoGames.CopyPaste.Analytics.Helpers;

namespace Iterations.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerCollision : MonoBehaviour
    {
        [Header("Death")]
        [SerializeField] private VoidEventChannelSO onLoseTriggered;
        [SerializeField] private string cloneTag = "Clone";

        [Header("Pickups")]
        [SerializeField] private IntEventChannelSO onPickupCollected;
        [SerializeField] private string pickupTag = "Pickup";

        [Header("Level Finish & Gate")]
        [SerializeField] private VoidEventChannelSO onWinTriggered;
        [SerializeField] private string finishTag = "Finish";
        [SerializeField] private VoidEventChannelSO onAllPickupsCollected;

        private int _pickupsCollected;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleContact(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other);
        }

        private void HandleContact(Collider2D other)
        {
            if (other.CompareTag(cloneTag))
            {
                int retries = GameManager.Instance != null ? GameManager.Instance.CurrentLevelRetries : 0;
                GameplayAnalytics.TrackPlayerDeath("clone_collision", SceneManager.GetActiveScene().name, retries);
                onLoseTriggered?.RaiseEvent();
                return;
            }

            if (other.CompareTag(pickupTag))
            {
                _pickupsCollected++;
                int required = LevelConfig.Instance != null ? LevelConfig.Instance.TotalPickups : 0;
                GameplayAnalytics.TrackPickupCollected("pickup", _pickupsCollected, required);
                onPickupCollected?.RaiseEvent(_pickupsCollected);
                Destroy(other.gameObject);

                if (_pickupsCollected == required)
                {
                    GameplayAnalytics.TrackAllPickupsCollected(SceneManager.GetActiveScene().name, _pickupsCollected);
                    onAllPickupsCollected?.RaiseEvent();
                }
                return;
            }

            if (other.CompareTag(finishTag))
            {
                TryFinishLevel();
            }
        }

        private void TryFinishLevel()
        {
            int required = LevelConfig.Instance != null ? LevelConfig.Instance.TotalPickups : 0;

            if (_pickupsCollected < required)
            {
                Debug.Log($"Need all pickups first: {_pickupsCollected}/{required}");
                return;
            }

            onWinTriggered?.RaiseEvent();
        }
    }
}