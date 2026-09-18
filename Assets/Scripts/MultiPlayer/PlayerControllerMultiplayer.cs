using System;
using UnityEngine;
using Unity.Netcode;
using Iterations.Events;

namespace Controller
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerControllerMultiplayer : NetworkBehaviour, IPlayerControllerMultiplayer
    {
        [SerializeField] private ScriptableStats _stats;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private bool JumpDown;
        private bool JumpHeld;
        private Vector2 _frameVelocity;

        public NetworkVariable<Vector2> Move;

        private bool _cachedQueryStartInColliders;

        private NetworkVariable<Color> _playerColor = new NetworkVariable<Color>(
            Color.white,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        #region Interface

        public Vector2 FrameInput => Move.Value;


        #endregion

        [Header("events Channels")]

        [SerializeField] private BoolEventChannelSO GroundedChanged;
        [SerializeField] private VoidEventChannelSO Jumped;

        private float _time;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _playerColor.OnValueChanged += OnColorChanged;

            if (IsOwner)
            {
                Color randomColor = UnityEngine.Random.ColorHSV(
                    0f, 1f,      // Hue
                    0.7f, 1f,    // Saturation
                    0.8f, 1f     // Brightness
                );

                SetPlayerColorServerRpc(randomColor);
            }

            ApplyPlayerColor(_playerColor.Value);
        }

        [ServerRpc]
        private void SetPlayerColorServerRpc(Color color)
        {
            _playerColor.Value = color;
        }

        public override void OnNetworkDespawn()
        {
            _playerColor.OnValueChanged -= OnColorChanged;

            base.OnNetworkDespawn();
        }


        private void OnColorChanged(Color oldColor, Color newColor)
        {
            ApplyPlayerColor(newColor);
        }

        private void ApplyPlayerColor(Color color)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
            }
        }


        private void Update()
        {
            if (GameManagerMultiplayer.Instance.GetCurrentStat() != GameManagerMultiplayer.State.GamePlaying) {
                GatherInputServerRpc(false, false, Vector2.zero);
                return;
            }
            _time += Time.deltaTime;

            if (!IsOwner) return;
            GatherInput();
        }

        Vector2 tempmove;
        private void GatherInput()
        {


            JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C);
            JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C);
            tempmove = new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
             
            

            if (_stats.SnapInput)
            {
                tempmove = new Vector2(Mathf.Abs(tempmove.x) <
                    _stats.HorizontalDeadZoneThreshold? 0: Mathf.Sign(tempmove.x),
                    Mathf.Abs(tempmove.y) <_stats.VerticalDeadZoneThreshold? 0: Mathf.Sign(tempmove.y));
            }

            if (JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }

            GatherInputServerRpc(JumpDown, JumpHeld, tempmove);
        }

        [ServerRpc]
        private void GatherInputServerRpc(bool jumpDown, bool jumpHeld, Vector2 move)
        {


            JumpDown = jumpDown;
            JumpHeld = jumpHeld;
            Move.Value = move;
            
            if (JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;

            CheckCollisions();
            HandleJump();
            HandleDirection();
            HandleGravity();

            ApplyMovement();
        }

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            bool groundHit = Physics2D.CapsuleCast(
                _col.bounds.center,
                _col.size,
                _col.direction,
                0,
                Vector2.down,
                _stats.GrounderDistance,
                ~_stats.PlayerLayer
            );

            bool ceilingHit = Physics2D.CapsuleCast(
                _col.bounds.center,
                _col.size,
                _col.direction,
                0,
                Vector2.up,
                _stats.GrounderDistance,
                ~_stats.PlayerLayer
            );

            if (ceilingHit)
            {
                _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);
            }

            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;

                GroundedChangedRaiseEventClientRpc(true);
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;

                GroundedChangedRaiseEventClientRpc(false);
            }

            Physics2D.queriesStartInColliders =
                _cachedQueryStartInColliders;
        }
        [ClientRpc] 
        void GroundedChangedRaiseEventClientRpc(bool value)
        {
            GroundedChanged.RaiseEvent(value);
        }
        #endregion


        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed = float.MinValue;

        private bool HasBufferedJump =>
            _bufferedJumpUsable &&
            _time < _timeJumpWasPressed + _stats.JumpBuffer;

        private bool CanUseCoyote =>
            _coyoteUsable &&
            !_grounded &&
            _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (
                !_endedJumpEarly &&
                !_grounded &&
                !JumpHeld &&
                _rb.linearVelocity.y > 0
            )
            {
                _endedJumpEarly = true;
            }

            if (!_jumpToConsume && !HasBufferedJump)
                return;

            if (_grounded || CanUseCoyote)
            {
                ExecuteJump();
            }

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = float.MinValue;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            _frameVelocity.y = _stats.JumpPower;

            JumpedRaiseEventClientRpc();
        }
        [ClientRpc]
        void JumpedRaiseEventClientRpc()
        {
            Jumped.RaiseEvent();
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (Move.Value.x == 0)
            {
                var deceleration =
                    _grounded
                        ? _stats.GroundDeceleration
                        : _stats.AirDeceleration;

                _frameVelocity.x = Mathf.MoveTowards(
                    _frameVelocity.x,
                    0,
                    deceleration * Time.fixedDeltaTime
                );
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(
                    _frameVelocity.x,
                    Move.Value.x * _stats.MaxSpeed,
                    _stats.Acceleration * Time.fixedDeltaTime
                );
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;

                if (
                    _endedJumpEarly &&
                    _frameVelocity.y > 0
                )
                {
                    inAirGravity *=
                        _stats.JumpEndEarlyGravityModifier;
                }

                _frameVelocity.y = Mathf.MoveTowards(
                    _frameVelocity.y,
                    -_stats.MaxFallSpeed,
                    inAirGravity * Time.fixedDeltaTime
                );
            }
        }

        #endregion

        private void ApplyMovement()
        {
            _rb.linearVelocity = _frameVelocity;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
            {
                Debug.LogWarning(
                    "Please assign a ScriptableStats asset to the Player Controller's Stats slot",
                    this
                );
            }
        }
#endif
    }
    public interface IPlayerControllerMultiplayer
    {
        public Vector2 FrameInput { get; }
    }
}