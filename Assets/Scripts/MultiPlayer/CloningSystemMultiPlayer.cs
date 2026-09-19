using Clone;
using Iterations.Events;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class CloningSystemMultiPlayer : NetworkBehaviour
{
    public Transform player { get; set; }

    [SerializeField] private GameObject ghostPrefabe;
    [SerializeField] private int firstCloneTime = 5;
    [SerializeField] private int cloneSpawningTime = 5;
    [SerializeField, Range(1, 10)] private int captureEveryNFrames = 2;
    [SerializeField] private int maxRecordTime = 500;
    [SerializeField] private ParticleSystem spawnEffect;

    [Header("events Channels")]
    [SerializeField] private VoidEventChannelSO onLoseTriggered;
    [SerializeField] private VoidEventChannelSO onJumpTriggered;
    [SerializeField] private BoolEventChannelSO GroundedChanged;

    private ReplaySystem _system;
    public override void OnNetworkSpawn()
    {
        
        if (!IsServer) return;

        _system = new ReplaySystem(this, true);

        if (onLoseTriggered != null)
        {
            onLoseTriggered.OnEventRaised += HandleOnLoseTriggered;
            if (onJumpTriggered != null) onJumpTriggered.OnEventRaised += RecordPlayerJump;
            if (GroundedChanged != null) GroundedChanged.OnEventRaised += RecordPlayerGrounded;
        }

       
    }
    public void StartRun()
    {
        _system.StartRun(player, captureEveryNFrames, maxRecordTime);
        InvokeRepeating(nameof(SpawnClone), firstCloneTime, cloneSpawningTime);
    }
    public override void OnNetworkDespawn()
    {
      
        if (IsServer)
        {
            CancelInvoke(nameof(SpawnClone));

            if (onLoseTriggered != null)
            {
                onLoseTriggered.OnEventRaised -= HandleOnLoseTriggered;
            }
        }
    }

   
    private void HandleOnLoseTriggered()
    {
        _system.FinishRun();
    }

    // Call this from your PlayerController script exactly when anim.SetTrigger("Jump") is called
    public void RecordPlayerJump()
    {
        _system?.NotifyPlayerJump();
    }

    // Call this from your PlayerController script exactly when anim.SetTrigger("Grounded") is called
    public void RecordPlayerGrounded(bool value)
    {
        if (value)
        {
            _system?.NotifyPlayerGrounded();
        }
    }

    void SpawnClone()
    {
        GameObject obj = Instantiate(ghostPrefabe);
        NetworkObject netObj = obj.GetComponent<NetworkObject>();


        netObj.Spawn(true);
        //obj.SetActive(true);
        _system.PlayRecording(obj);
        SpawnEffectClientRpc(netObj);
    }

    [ClientRpc]
    void SpawnEffectClientRpc(NetworkObjectReference objRef)
    {
        if(objRef.TryGet(out NetworkObject netObj))
        {
            GameObject obj = netObj.gameObject;
            StartCoroutine(SpawnEffect(obj));
        }
        else
        {
            Debug.LogError("cant get the object from the ref");
        }
    }

    IEnumerator SpawnEffect(GameObject obj)
    {
        yield return new WaitForSeconds(0.05f);
        obj.GetComponentInChildren<SpriteRenderer>().enabled = true;
        if (spawnEffect != null)
        {
            Instantiate(spawnEffect, obj.transform.position, Quaternion.identity);
        }
    }
}
