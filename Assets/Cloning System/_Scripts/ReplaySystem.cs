using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Clone {
    public class ReplaySystem {
        private readonly WaitForFixedUpdate _wait = new WaitForFixedUpdate();

        public ReplaySystem(MonoBehaviour System) {
            _replaySmoothedTimes = new List<float>();
            _ghostObjs = new List<GameObject>();
            System.StartCoroutine(FixedUpdate());
            System.StartCoroutine(Update());
        }

        private IEnumerator FixedUpdate() {
            while (true) {
                yield return _wait;
                AddSnapshot();
                _elapsedRecordingTime += Time.smoothDeltaTime;
            }
        }

        private IEnumerator Update() {
            while (true) {
                yield return null;
                for(int i =0; i < _replaySmoothedTimes.Count; i++)
                {
                    _replaySmoothedTimes[i] += Time.smoothDeltaTime;

                }
                UpdateReplays();
            }
        }

        #region Recording

        //private readonly Dictionary<RecordingType, Recording> _runs = new Dictionary<RecordingType, Recording>();
        private Recording _currentRun;
        private float _elapsedRecordingTime;
        private int _snapshotEveryNFrames;
        private int _frameCount;
        private float _maxRecordingTimeLimit;

        /// <summary>
        /// Begin recording a run
        /// </summary>
        /// <param name="target">The transform you wish to record</param>
        /// <param name="snapshotEveryNFrames">The accuracy of the recording. Smaller number == higher file size</param>
        /// <param name="maxRecordingTimeLimit">Stop recording beyond this time</param>
        public void StartRun(Transform target, int snapshotEveryNFrames = 2, float maxRecordingTimeLimit = 60)
        {
            if (_currentRun != null) Debug.LogError("Cant create another record??");
            _currentRun = new Recording(target);


            _elapsedRecordingTime = 0;

            _snapshotEveryNFrames = Mathf.Max(1, snapshotEveryNFrames);
            _frameCount = 0;

            _maxRecordingTimeLimit = maxRecordingTimeLimit;
        }

        private void AddSnapshot() {
            if (_currentRun == null) return;

            // Capture frame, taking into account the frame skip
            if (_frameCount++ % _snapshotEveryNFrames == 0) _currentRun.AddSnapshot(_elapsedRecordingTime);

            // End a run over the limit
            if (_currentRun.Duration >= _maxRecordingTimeLimit) FinishRun();
        }

        /// <summary>
        /// Complete the current recording
        /// </summary>
        /// <param name="save">If we want to save this run. Use false for restarts</param>
        /// <returns>Whether this run was the fastest so far</returns>
        public bool FinishRun(bool save = true)
        {
            if (_currentRun == null) return false;
            _currentRun = null;

            return true;
        }

        /// <summary>
        /// Set the saved run. This can be pulled from leaderboards or friends, etc
        /// </summary>
        /// <param name="run">The run you'd like to set for playback</param>
      //  public void SetSavedRun(Recording run) => _runs[RecordingType.Saved] = run;

        /// <summary>
        /// Retrieve a run
        /// </summary>
        /// <param name="type">The type of run you'd like to retrieve</param>
        /// <param name="run">The resulting run</param>
        /// <returns></returns>
        //public bool GetRun(RecordingType type, out Recording run) {
        //    return _runs.TryGetValue(type, out run);
        //}

        #endregion

        #region Play Ghost

        private Recording _currentReplay;
        private List<GameObject> _ghostObjs;
        private bool _destroyOnComplete;
        private List<float> _replaySmoothedTimes;

        /// <summary>
        /// Begin playing a recording
        /// </summary>
        /// <param name="ghostObj">The visual representation of the ghost. Must be pre-instantiated (this allows customization)</param>
        /// <param name="destroyOnCompletion">Whether or not to automatically destroy the ghost object when the run completes</param>
        public void PlayRecording( GameObject ghostObj, bool destroyOnCompletion = true) {
            //if (_ghostObjs[_ghostObjs.Count -1] != null) Object.Destroy(_ghostObj);

            if (_currentRun == null) {
                Object.Destroy(ghostObj);
                return;
            }
            _currentReplay = _currentRun;
            _replaySmoothedTimes.Add(0f);

            _destroyOnComplete = destroyOnCompletion;

            if (_currentReplay != null) _ghostObjs.Add(ghostObj);
            else if (_destroyOnComplete) Object.Destroy(ghostObj);
        }

        private void UpdateReplays() {
            if (_currentReplay == null) return;
            for (int i = 0; i < _replaySmoothedTimes.Count; i++)
            {
                // Evaluate the point at the current time
                var pose = _currentReplay.EvaluatePoint(_replaySmoothedTimes[i]);
                _ghostObjs[i].transform.SetPositionAndRotation(pose.position, pose.rotation);

                // Destroy the replay when done
                if (_replaySmoothedTimes[i] > _currentReplay.Duration)
                {
                    _currentReplay = null;
                    if (_destroyOnComplete) Object.Destroy(_ghostObjs[i]);
                }
            }
        }

        /// <summary>
        /// Stop the replay. Should be called when the player finishes the run before the ghost
        /// </summary>
        //public void StopReplay(int index) {
        //    if (_ghostObjs[index] != null) Object.Destroy(_ghostObjs[index]);
        //    _currentReplay = null;
        //}

        #endregion
    }

}