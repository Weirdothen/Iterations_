using System;
using UnityEngine;
using Unity.Netcode;

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerControllerMultiplayer : NetworkBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;

        private NetworkVariable<Color> _playerColor = new NetworkVariable<Color>(
            Color.white,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion

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
            _time += Time.deltaTime;

            if (!IsOwner) return;
            GatherInput();
        }


        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
                Move = new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                )
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x =
                    Mathf.Abs(_frameInput.Move.x) <
                    _stats.HorizontalDeadZoneThreshold
                        ? 0
                        : Mathf.Sign(_frameInput.Move.x);

                _frameInput.Move.y =
                    Mathf.Abs(_frameInput.Move.y) <
                    _stats.VerticalDeadZoneThreshold
                        ? 0
                        : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }

            GatherInputServerRpc(_frameInput.JumpDown, _frameInput.JumpHeld, _frameInput.Move);
        }

        [ServerRpc]
        private void GatherInputServerRpc(bool jumpDown, bool jumpHeld, Vector2 move)
        {
            _frameInput = new FrameInput
            {
                JumpDown = jumpDown,
                JumpHeld = jumpHeld,
                Move = move
            };
            if (_frameInput.JumpDown)
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

                GroundedChanged?.Invoke(
                    true,
                    Mathf.Abs(_frameVelocity.y)
                );
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;

                GroundedChanged?.Invoke(false, 0);
            }

            Physics2D.queriesStartInColliders =
                _cachedQueryStartInColliders;
        }

        #endregion


        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

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
                !_frameInput.JumpHeld &&
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
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            _frameVelocity.y = _stats.JumpPower;

            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
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
                    _frameInput.Move.x * _stats.MaxSpeed,
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
}