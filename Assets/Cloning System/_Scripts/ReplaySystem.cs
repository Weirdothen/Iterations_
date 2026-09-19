using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Clone
{
    public class ReplaySystem
    {
        private readonly WaitForFixedUpdate _wait = new WaitForFixedUpdate();
        private readonly MonoBehaviour _owner;
        private static readonly int GroundedParam = Animator.StringToHash("Grounded");
        private static readonly int JumpParam = Animator.StringToHash("Jump");
        private static readonly int WalkKey = Animator.StringToHash("Walk");

        private bool _wasJumpTriggeredThisFrame;
        private bool _wasGroundedTriggeredThisFrame;

        public ReplaySystem(MonoBehaviour system, bool usePhysics)
        {
            _owner = system;
            this.usePhysics = usePhysics;
            _replaySmoothedTimes = new List<float>();
            _previousReplayTimes = new List<float>();
            _ghostObjs = new List<GameObject>();
            _ghostRbs = new List<Rigidbody2D>();
            _ghostAnimators = new List<Animator>();

            system.StartCoroutine(FixedUpdate());
            system.StartCoroutine(Update());
        }

        // Methods to receive trigger signals from the player
        public void NotifyPlayerJump() => _wasJumpTriggeredThisFrame = true;
        public void NotifyPlayerGrounded() => _wasGroundedTriggeredThisFrame = true;

        private IEnumerator FixedUpdate()
        {
            while (true)
            {
                yield return _wait;
                AddSnapshot();
                _elapsedRecordingTime += Time.smoothDeltaTime;

                // Reset triggers after they have been recorded for the frame
                _wasJumpTriggeredThisFrame = false;
                _wasGroundedTriggeredThisFrame = false;
            }
        }

        private IEnumerator Update()
        {
            while (true)
            {
                yield return null;
                for (int i = 0; i < _replaySmoothedTimes.Count; i++)
                {
                    _previousReplayTimes[i] = _replaySmoothedTimes[i];
                    _replaySmoothedTimes[i] += Time.smoothDeltaTime;
                }
                UpdateReplays();
            }
        }

        #region Recording

        private Recording _currentRun;
        private float _elapsedRecordingTime;
        private int _snapshotEveryNFrames;
        private int _frameCount;
        private float _maxRecordingTimeLimit;
        private bool usePhysics;
        private bool isRecored = false;

        public void StartRun(Transform target, int snapshotEveryNFrames = 2, float maxRecordingTimeLimit = 60)
        {
            if (_currentRun != null) Debug.LogError("Cant create another record??");
            _currentRun = new Recording(target);
            isRecored = true;

            _elapsedRecordingTime = 0;
            _snapshotEveryNFrames = Mathf.Max(1, snapshotEveryNFrames);
            _frameCount = 0;
            _maxRecordingTimeLimit = maxRecordingTimeLimit;
        }

        private void AddSnapshot()
        {
            if (_currentRun == null || !isRecored) return;

            if (_frameCount++ % _snapshotEveryNFrames == 0)
            {
                _currentRun.AddSnapshot(_elapsedRecordingTime, _wasJumpTriggeredThisFrame, _wasGroundedTriggeredThisFrame);
            }

            if (_currentRun.Duration >= _maxRecordingTimeLimit) FinishRun();
        }

        public bool FinishRun()
        {
            if (_currentRun == null) return false;
            //_currentRun = null;
            isRecored = false;
            return true;
        }

        #endregion

        #region Play Ghost

        private Recording _currentReplay;
        private List<GameObject> _ghostObjs;
        private List<Rigidbody2D> _ghostRbs;
        private List<Animator> _ghostAnimators;
        private List<float> _replaySmoothedTimes;
        private List<float> _previousReplayTimes;
        private bool _destroyOnComplete;

        public void PlayRecording(GameObject ghostObj, bool destroyOnCompletion = true)
        {
            if (_currentRun == null)
            {
                Object.Destroy(ghostObj);
                return;
            }

            _currentReplay = _currentRun;
            _replaySmoothedTimes.Add(0f);
            _previousReplayTimes.Add(0f);
            _destroyOnComplete = destroyOnCompletion;

            if (_currentReplay != null)
            {
                if (usePhysics)
                {
                    if (ghostObj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
                    {
                        _ghostRbs.Add(rb);
                    }
                }
                Animator anim = ghostObj.GetComponentInChildren<Animator>();
                if (anim !=null)
                {
                    _ghostAnimators.Add(anim);
                    Debug.Log("found");
                }
                else
                {
                    _ghostAnimators.Add(null);
                }

                _ghostObjs.Add(ghostObj);
            }
            else if (_destroyOnComplete)
            {
                Object.Destroy(ghostObj);
            }
        }

        private void UpdateReplays()
        {
            if (_currentReplay == null) return;

            for (int i = 0; i < _replaySmoothedTimes.Count; i++)
            {
                var pose = _currentReplay.EvaluatePoint(_replaySmoothedTimes[i]);

                
                if (usePhysics && _ghostRbs.Count > i && _ghostRbs[i] != null)
                {
                    _ghostRbs[i].position = pose.position;
                    _ghostRbs[i].SetRotation(pose.rotation);
                }
                else if (_ghostObjs.Count > i && _ghostObjs[i] != null)
                {
                    _ghostObjs[i].transform.SetPositionAndRotation(pose.position, pose.rotation);
                }

                if (_ghostAnimators.Count > i && _ghostAnimators[i] != null)
                {
                    _ghostAnimators[i].SetFloat(WalkKey, _currentReplay.EvaluateAnimation(_replaySmoothedTimes[i]));

                    // Fire Jump Trigger if timestamp crossed
                    if (_currentReplay.HasJumpedInTimeRange(_previousReplayTimes[i], _replaySmoothedTimes[i]))
                    {
                        _ghostAnimators[i].SetTrigger(JumpParam);
                    }

                    // Fire Grounded Trigger if timestamp crossed
                    if (_currentReplay.HasGroundedInTimeRange(_previousReplayTimes[i], _replaySmoothedTimes[i]))
                    {
                        _ghostAnimators[i].SetTrigger(GroundedParam);
                    }
                }

                if (_replaySmoothedTimes[i] > _currentReplay.Duration)
                {
                    if (_destroyOnComplete && _ghostObjs[i] != null)
                    {
                        if (_ghostObjs[i].TryGetComponent<NetworkObject>(out NetworkObject networkObject))
                        {
                            networkObject.Despawn();
                        }
                        else
                        {
                            Object.Destroy(_ghostObjs[i]);
                        }
                    }
                }
            }
        }

        #endregion
    }
}