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

        [Header("Effects")]
        [SerializeField] private AudioSource playerAudioSource;
        [SerializeField] private AudioClip deathSound;
        [SerializeField] private PlayerDeathEffect deathEffect;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private GameObject pickupParticle;


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
            if (!IsServer) return;

            if (other.CompareTag(cloneTag))
            {
                onLoseTriggered?.RaiseEvent();
                PlayDeathEffectsClientRpc();
                DespawnPlayerServerRpc();
                return;
            }

            if (other.CompareTag(pickupTag))
            {
                _pickupsCollected++;
                onPickupCollected?.RaiseEvent(_pickupsCollected);
                PlayPickupEffectsClientRpc();

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
            
        }

        [ClientRpc]
        private void PlayDeathEffectsClientRpc()
        {
            if (deathEffect != null)
            {
                deathEffect.TriggerExplosion(transform.position);
            }

            // Both players hear the death sound if anyone dies
            if (playerAudioSource != null && deathSound != null)
            {
                playerAudioSource.PlayOneShot(deathSound);
            }
        }

        [ClientRpc]
        private void PlayPickupEffectsClientRpc()
        {
            if (pickupParticle != null)
            {
                Instantiate( pickupParticle, transform.position,Quaternion.identity);
            }

            // Only the player who picked it up hears the sound
            if (IsOwner && playerAudioSource != null && pickupSound != null)
            {
                playerAudioSource.PlayOneShot(pickupSound);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DespawnPlayerServerRpc()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DespawnObjectServerRpc(ulong objectId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
            {
                netObj.Despawn(true);
            }
        }
    }
}