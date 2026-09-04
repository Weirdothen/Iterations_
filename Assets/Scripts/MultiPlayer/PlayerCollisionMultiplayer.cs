using UnityEngine;
using Unity.Netcode;
using Iterations.Events;
using Iterations.Core;

namespace Iterations.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerCollisionMultiplayer : NetworkBehaviour
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
            if (!IsOwner) return;

            if (other.CompareTag(cloneTag))
            {
                onLoseTriggered?.RaiseEvent();
                DespawnPlayerServerRpc();
                return;
            }

            if (other.CompareTag(pickupTag))
            {
                _pickupsCollected++;
                onPickupCollected?.RaiseEvent(_pickupsCollected);

                NetworkObject pickupNetObj = other.GetComponent<NetworkObject>();
                if (pickupNetObj != null)
                {
                    DespawnObjectServerRpc(pickupNetObj.NetworkObjectId);
                }
                else
                {
                    Destroy(other.gameObject);
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

        [ServerRpc]
        private void DespawnPlayerServerRpc()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }

        [ServerRpc]
        private void DespawnObjectServerRpc(ulong objectId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
            {
                netObj.Despawn(true);
            }
        }
    }
}