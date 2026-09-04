using Clone;
using UnityEngine;
using Iterations.Events;

public class CloningSystem : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject ghostPrefabe;
    [SerializeField] private int firstCloneTime= 5;
    [SerializeField] private int cloneSpawningTime = 5;
    [SerializeField, Range(1, 10)] private int captureEveryNFrames = 2;
    [SerializeField] private int maxRecordTime = 500;

    [Header("events Channels")]
    [SerializeField] private VoidEventChannelSO onLoseTriggered;

    private ReplaySystem _system;


    private void Awake()
    {
        _system = new ReplaySystem(this);
    }

    private void OnEnable()
    { 
        if (onLoseTriggered != null) onLoseTriggered.OnEventRaised += HandleOnLoseTriggered;
    }

    private void HandleOnLoseTriggered()
    {
        _system.FinishRun();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _system.StartRun(player, captureEveryNFrames, maxRecordTime);
        InvokeRepeating("SpawnClone", firstCloneTime, cloneSpawningTime);
    }
    void SpawnClone()
    {
        GameObject obj = Instantiate(ghostPrefabe);
        _system.PlayRecording(obj);
    }
}
