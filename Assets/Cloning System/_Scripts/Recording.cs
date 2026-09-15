using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Clone
{
    public class Recording
    {
        private readonly AnimationCurve _posXCurve = new AnimationCurve();
        private readonly AnimationCurve _posYCurve = new AnimationCurve();
        private readonly AnimationCurve _rotZCurve = new AnimationCurve();

        private readonly AnimationCurve _AnimationWalk = new AnimationCurve();

        // Lists to store the exact time a trigger occurred
        private readonly List<float> _jumpTimestamps = new List<float>();
        private readonly List<float> _groundedTimestamps = new List<float>();

        public float Duration { get; private set; }
        private readonly Transform _target;

        private Animator _animator;
        public Recording(Transform target)
        {
            _target = target;
            _animator = target.GetComponentInChildren<Animator>();
        }

        public void AddSnapshot(float elapsed, bool jumpTriggered, bool groundedTriggered)
        {
            Duration = elapsed;

            var pos = _target.position;
            var rot = _target.rotation.eulerAngles;

            UpdateCurve(_posXCurve, elapsed, pos.x);
            UpdateCurve(_posYCurve, elapsed, pos.y);
            UpdateCurve(_rotZCurve, elapsed, rot.z);
            UpdateCurve(_AnimationWalk, elapsed, _animator.GetFloat(WalkKey));

            if (jumpTriggered) _jumpTimestamps.Add(elapsed);
            if (groundedTriggered) _groundedTimestamps.Add(elapsed);

            void UpdateCurve(AnimationCurve curve, float time, float val)
            {
                var count = curve.length;
                var kf = new Keyframe(time, val);

                if (count > 1 &&
                    Mathf.Approximately(curve.keys[count - 1].value, curve.keys[count - 2].value) &&
                    Mathf.Approximately(val, curve.keys[count - 1].value))
                {
                    curve.MoveKey(count - 1, kf);
                }
                else
                {
                    curve.AddKey(kf);
                }
            }
        }

        public Pose EvaluatePoint(float elapsed) => new Pose(
            new Vector3(_posXCurve.Evaluate(elapsed), _posYCurve.Evaluate(elapsed), 0f),
            Quaternion.Euler(0, 0, _rotZCurve.Evaluate(elapsed)));

        public float EvaluateAnimation(float elapsed)
        {
            return _AnimationWalk.Evaluate(elapsed);
        }
        public bool HasJumpedInTimeRange(float prevTime, float currentTime) =>
            CheckTrigger(_jumpTimestamps, prevTime, currentTime);

        public bool HasGroundedInTimeRange(float prevTime, float currentTime) =>
            CheckTrigger(_groundedTimestamps, prevTime, currentTime);

        private bool CheckTrigger(List<float> timestamps, float prevTime, float currentTime)
        {
            for (int i = 0; i < timestamps.Count; i++)
            {
                if (timestamps[i] > prevTime && timestamps[i] <= currentTime)
                {
                    return true;
                }
            }
            return false;
        }
        private static readonly int WalkKey = Animator.StringToHash("Walk");
    }

    
}