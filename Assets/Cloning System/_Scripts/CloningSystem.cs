using Clone;
using UnityEngine;
using System.Collections;
using Iterations.Events;
using Unity.VisualScripting;

public class CloningSystem : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject ghostPrefabe;
    [SerializeField] private ParticleSystem spawnEffect;
    [SerializeField] private int firstCloneTime = 5;
    [SerializeField] private int cloneSpawningTime = 5;
    [SerializeField, Range(1, 10)] private int captureEveryNFrames = 2;
    [SerializeField] private int maxRecordTime = 500;

    [Header("events Channels")]
    [SerializeField] private VoidEventChannelSO onLoseTriggered;
    [SerializeField] private VoidEventChannelSO onJumpTriggered;
    [SerializeField] private BoolEventChannelSO GroundedChanged;

    private ReplaySystem _system;

    private void Awake()
    {
        _system = new ReplaySystem(this, true);
    }

    private void OnEnable()
    {
        if (onLoseTriggered != null) onLoseTriggered.OnEventRaised += HandleOnLoseTriggered;
        if (onJumpTriggered != null) onJumpTriggered.OnEventRaised += RecordPlayerJump;
        if (GroundedChanged != null) GroundedChanged.OnEventRaised += RecordPlayerGrounded;
        
    }

    private void OnDisable()
    {
        if (onLoseTriggered != null) onLoseTriggered.OnEventRaised -= HandleOnLoseTriggered;
    }

    private void HandleOnLoseTriggered()
    {
        _system.FinishRun();
    }

    void Start()
    {
        _system.StartRun(player, captureEveryNFrames, maxRecordTime);
        InvokeRepeating("SpawnClone", firstCloneTime, cloneSpawningTime);
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
        _system.PlayRecording(obj);
        StartCoroutine(SpawnEffect(obj));
    }

    IEnumerator SpawnEffect(GameObject obj)
    {
        yield return new WaitForSeconds(0.02f);
        if (spawnEffect != null)
        {
            Instantiate(spawnEffect, obj.transform.position, Quaternion.identity);
        }
    }
}