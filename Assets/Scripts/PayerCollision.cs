using UnityEngine;
using Iterations.Events;

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

        [Header("Level Finish")]
        [SerializeField] private VoidEventChannelSO onWinTriggered;
        [SerializeField] private string finishTag = "Finish";

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
                onLoseTriggered?.RaiseEvent();
                return;
            }

            if (other.CompareTag(pickupTag))
            {
                _pickupsCollected++;
                onPickupCollected?.RaiseEvent(_pickupsCollected);
                Destroy(other.gameObject);
                return;
            }

            if (other.CompareTag(finishTag))
            {
                onWinTriggered?.RaiseEvent();
            }
        }
    }
}