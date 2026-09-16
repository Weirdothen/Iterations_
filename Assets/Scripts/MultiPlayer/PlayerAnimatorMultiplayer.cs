using UnityEngine;
using Unity.Netcode;
using Iterations.Events;

namespace TarodevController
{
    public class PlayerAnimatorMultiplayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Animator _anim;

        [SerializeField] private SpriteRenderer _sprite;

        [Header("Settings")]
        [SerializeField] private float landImpact = 20;


        [Header("Particles")][SerializeField] private ParticleSystem _jumpParticles;
        [SerializeField] private ParticleSystem _launchParticles;
        [SerializeField] private ParticleSystem _moveParticles;
        [SerializeField] private ParticleSystem _landParticles;

        [Header("Audio Clips")]
        [SerializeField]
        private AudioClip[] _footsteps;
        [Header("events Channels")]

        [SerializeField] private BoolEventChannelSO GroundedChanged;
        [SerializeField] private VoidEventChannelSO Jumped;

        private AudioSource _source;
        private IPlayerControllerMultiplayer _player;
        private bool _grounded;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _player = GetComponentInParent<IPlayerControllerMultiplayer>();
            
        }

        private void OnEnable()
        {

            if (_player != null)
            {
                Jumped.OnEventRaised += OnJumped;
                GroundedChanged.OnEventRaised += OnGroundedChanged;
            }

            _moveParticles.Play();
        }

        private void OnDisable()
        {

            if (_player != null)
            {
                Jumped.OnEventRaised -= OnJumped;
                GroundedChanged.OnEventRaised -= OnGroundedChanged;
            }

            _moveParticles.Stop();
        }

        private void Update()
        {
            if (_player == null) return;

            HandleSpriteFlip();

            HandleIdleSpeed();

        }

        private void HandleSpriteFlip()
        {
            if (_player.FrameInput.x != 0) _sprite.flipX = _player.FrameInput.x < 0;
        }

        private void HandleIdleSpeed()
        {
            var inputStrength = Mathf.Abs(_player.FrameInput.x);
            _anim.SetFloat(WalkKey, inputStrength);
            _moveParticles.transform.localScale = Vector3.MoveTowards(_moveParticles.transform.localScale, Vector3.one * inputStrength, 2 * Time.deltaTime);
        }



        private void OnJumped()
        {
            OnjumpedClientRpc();
        }
        [ClientRpc] 
        private void OnjumpedClientRpc()
        {
            _anim.SetTrigger(JumpKey);
            _anim.ResetTrigger(GroundedKey);


            if (_grounded) // Avoid coyote
            {
                _jumpParticles.Play();
            }
        }

        private void OnGroundedChanged(bool grounded)
        {
            OnGroundedChangedClientRpc(grounded);
        }
        [ClientRpc]
        void OnGroundedChangedClientRpc(bool grounded )
        {
            _grounded = grounded;

            if (grounded)
            {

                _anim.SetTrigger(GroundedKey);

                if (_footsteps != null && _footsteps.Length > 0)
                {
                    _source.PlayOneShot(_footsteps[Random.Range(0, _footsteps.Length)]);
                }

                _moveParticles.Play();

                _landParticles.transform.localScale = Vector3.one * Mathf.InverseLerp(0, 40, landImpact);
                _landParticles.Play();
            }
            else
            {
                _moveParticles.Stop();
            }
        }

        private static readonly int GroundedKey = Animator.StringToHash("Grounded");
        private static readonly int WalkKey = Animator.StringToHash("Walk");
        private static readonly int JumpKey = Animator.StringToHash("Jump");
    }
}