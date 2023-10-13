using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerController;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    //[SerializeField]
    //private Animator _anim;
    [SerializeField] private Transform _spriteTransform;

    [Header("Particles")][SerializeField] private ParticleSystem _jumpParticles;
    [SerializeField] private ParticleSystem _wallJumpParticles;
    [SerializeField] private ParticleSystem _moveParticles;
    [SerializeField] private ParticleSystem _landParticles;
    [SerializeField] private ParticleSystem _doubleJumpParticles;

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip _doubleJumpClip;

    [SerializeField] private AudioClip[] _jumpClips;
    [SerializeField] private AudioClip[] _splats;
    [SerializeField] private AudioClip[] _slideClips;

    private AudioSource _source;
    private PlayerController _player;
    private Vector3 _spriteSize;
    private int _wallDir;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _player = GetComponent<PlayerController>();
        _spriteSize = _spriteTransform.localScale;
    }

    private void OnEnable()
    {
        _player.Jumped += OnJumped;
        _player.GroundedChanged += OnGroundedChanged;
        _player.WallGrabChanged += OnWallGrabChanged;
        _player.HeadBumb += OnHeadBumb;

        if (_moveParticles != null && _player.SampleIsGrounded())
            _moveParticles.Play();
    }

    private void OnDisable()
    {
        _player.Jumped -= OnJumped;
        _player.GroundedChanged -= OnGroundedChanged;
        _player.WallGrabChanged -= OnWallGrabChanged;
        _player.HeadBumb -= OnHeadBumb;

        if (_moveParticles != null)
            _moveParticles.Stop();
    }

    private void Update()
    {
        if (_player == null) return;

        var xInput = _player.Input.x;

        HandleIdleSpeed(xInput);

        HandleCharacterTilt(xInput);

        HandleWallSlideEffects();
    }

    #region Squish

    [Header("Squish")][SerializeField] private ParticleSystem.MinMaxCurve _squishMinMaxX;
    [SerializeField] private ParticleSystem.MinMaxCurve _squishMinMaxY;
    [SerializeField] private float _minSquishForce = 6f;
    [SerializeField] private float _maxSquishForce = 30f;
    [SerializeField] private float _minSquishDuration = 0.1f;
    [SerializeField] private float _maxSquishDuration = 0.4f;
    private bool _isSquishing;

    private IEnumerator SquishPlayer(float force)
    {
        force = Mathf.Abs(force);
        if (force < _minSquishForce) yield break;
        _isSquishing = true;

        var elapsedTime = 0f;

        //var point = Mathf.InverseLerp(_minSquishForce, _maxSquishForce, force);
        var point = Mathf.Max(0, force - _minSquishForce) / (_maxSquishForce - _minSquishForce);
        var duration = Mathf.Lerp(_minSquishDuration, _maxSquishDuration, point);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            var t = elapsedTime / duration;

            var squishFactorY = Mathf.Lerp(_squishMinMaxY.curveMax.Evaluate(t), _squishMinMaxY.curveMin.Evaluate(t), point);
            var squishFactorX = Mathf.Lerp(_squishMinMaxX.curveMax.Evaluate(t), _squishMinMaxX.curveMin.Evaluate(t), point);
            _spriteTransform.localScale = new Vector3(_spriteSize.x * squishFactorX, _spriteSize.y * squishFactorY);

            yield return null;
        }

        _spriteTransform.localScale = _spriteSize;
        _isSquishing = false;
    }

    private void CancelSquish()
    {
        _isSquishing = false;
        _spriteTransform.localScale = _spriteSize;
        if (_squishRoutine != null) StopCoroutine(_squishRoutine);
    }

    #endregion

    #region Animation

    [Header("Idle")]
    [SerializeField, Range(1f, 3f)]
    private float _maxIdleSpeed = 2;

    // Speed up idle while running
    private void HandleIdleSpeed(float xInput)
    {
        var inputStrength = Mathf.Abs(xInput);
        //_anim.SetFloat(IdleSpeedKey, Mathf.Lerp(1, _maxIdleSpeed, inputStrength));
        if (_moveParticles != null)
            _moveParticles.transform.localScale = Vector3.MoveTowards(_moveParticles.transform.localScale,
                Vector3.one * inputStrength, 2 * Time.deltaTime);
    }

    #endregion

    #region Tilt

    [Header("Tilt")][SerializeField] private float _runningTilt = 5; // In degrees around the Z axis
    [SerializeField] private float _maxTilt = 10; // In degrees around the Z axis
    [SerializeField] private float _tiltSmoothTime = 0.1f;

    private Vector3 _currentTiltVelocity;

    private void HandleCharacterTilt(float xInput)
    {
        var runningTilt = _grounded ? Quaternion.Euler(0, 0, _runningTilt * xInput) : Quaternion.identity;
        var targetRot = runningTilt * _player.Up;

        // Calculate the smooth damp effect
        var smoothRot = Vector3.SmoothDamp(_spriteTransform.up, targetRot, ref _currentTiltVelocity, _tiltSmoothTime);

        if (Vector3.Angle(_player.Up, smoothRot) > _maxTilt)
        {
            smoothRot = Vector3.RotateTowards(_player.Up, smoothRot, Mathf.Deg2Rad * _maxTilt, 0f);
        }

        // Rotate towards the smoothed target
        _spriteTransform.up = smoothRot;
    }

    #endregion

    #region Event Callbacks

    private void OnJumped(JumpType type)
    {
        if (type is JumpType.Jump or JumpType.Coyote or JumpType.WallJump)
        {
            //_anim.SetTrigger(JumpKey);
            //_anim.ResetTrigger(GroundedKey);
            PlayRandomSound(_jumpClips, 0.2f, UnityEngine.Random.Range(0.98f, 1.02f));

            // Only play particles when grounded (avoid coyote)
            if (_jumpParticles != null && type is JumpType.Jump)
            {
                _jumpParticles.Play();
            }
            else if (_wallJumpParticles != null && type is JumpType.WallJump)
            {
                _wallJumpParticles.transform.localPosition = new Vector3(_wallSlideParticleOffset * _wallDir, 0, 0);
                if(_wallDir < 0) _wallJumpParticles.transform.eulerAngles = new Vector3(0, 90, -90);
                else             _wallJumpParticles.transform.eulerAngles = new Vector3(0, -90, 90);
                _wallJumpParticles.Play();
            }
        }
        else if (type is JumpType.AirJump)
        {
            if (_source != null)
                _source.PlayOneShot(_doubleJumpClip);
            if (_doubleJumpParticles != null)
                _doubleJumpParticles.Play();
        }
    }

    private bool _grounded;
    private Coroutine _squishRoutine;

    private void OnGroundedChanged(bool grounded, float impact)
    {
        _grounded = grounded;

        if (grounded)
        {
            //_anim.SetBool(GroundedKey, true);
            CancelSquish();
            _squishRoutine = StartCoroutine(SquishPlayer(Mathf.Abs(impact)));
            if (_source != null)
                _source.PlayOneShot(_splats[UnityEngine.Random.Range(0, _splats.Length)]);
            if (_moveParticles != null)
                _moveParticles.Play();

            if (_landParticles != null)
            {
                _landParticles.transform.localScale = Vector3.one * Mathf.InverseLerp(0, 40, impact);
                _landParticles.Play();
            }
        }
        else
        {
            //_anim.SetBool(GroundedKey, false);
            if (_moveParticles != null)
                _moveParticles.Stop();
        }
    }

    private void OnHeadBumb(float impact)
    {
        CancelSquish();
        _squishRoutine = StartCoroutine(SquishPlayer(Mathf.Abs(impact)));
    }

    #endregion

    #region Walls & Ladders

    [Header("Walls & Ladders")]
    [SerializeField]
    private ParticleSystem _wallSlideParticles;

    [SerializeField] private AudioSource _wallSlideSource;
    [SerializeField] private AudioClip[] _wallClimbClips;
    [SerializeField] private AudioClip[] _ladderClimbClips;
    [SerializeField] private float _maxWallSlideVolume = 0.2f;
    [SerializeField] private float _wallSlideParticleOffset = 0.3f;
    [SerializeField] private float _distancePerClimbSound = 0.2f;

    private bool _isOnWall, _isSliding;
    private float _slideAudioVel;
    private bool _ascendingLadder;
    private float _lastClimbSoundY;
    private int _wallClimbAudioIndex = 0;
    private int _ladderClimbAudioIndex;

    private void OnWallGrabChanged(bool onWall)
    {
        _isOnWall = onWall;
        if(_isOnWall)
            _wallDir = _player.WallDirection;
    }

    private void HandleWallSlideEffects()
    {
        var slidingThisFrame = _isOnWall && !_grounded && _player.Velocity.y < 0;

        if (!_isSliding && slidingThisFrame)
        {
            _isSliding = true;
            if (_wallSlideParticles != null)
                _wallSlideParticles.Play();
        }
        else if (_isSliding && !slidingThisFrame)
        {
            _isSliding = false;
            if (_wallSlideParticles != null)
                _wallSlideParticles.Stop();
        }

        if (_wallSlideParticles != null)
        {
            _wallSlideParticles.transform.localPosition = new Vector3(_wallSlideParticleOffset * _wallDir, 0, 0);
        }

        if (_wallSlideSource != null)
        {
            var requiredAudio = _isSliding;
            var point = requiredAudio ? Mathf.InverseLerp(0, -_player.Stats.WallFallSpeed, _player.Velocity.y) : 0;
            _wallSlideSource.volume = Mathf.SmoothDamp(_wallSlideSource.volume, Mathf.Lerp(0, _maxWallSlideVolume, point), ref _slideAudioVel, 0.2f);
        }

        if (_isOnWall && _player.Velocity.y > 0)
        {
            if (!_ascendingLadder)
            {
                _ascendingLadder = true;
                _lastClimbSoundY = transform.position.y;
                Play();
            }

            if (transform.position.y >= _lastClimbSoundY + _distancePerClimbSound)
            {
                _lastClimbSoundY = transform.position.y;
                Play();
            }
        }
        else
        {
            _ascendingLadder = false;
        }

        void Play()
        {
            if (_isOnWall) PlayWallClimbSound();
            else PlayLadderClimbSound();
        }
    }

    private void PlayWallClimbSound()
    {
        if (_wallClimbClips.Length == 0) return;
        _wallClimbAudioIndex = (_wallClimbAudioIndex + 1) % _wallClimbClips.Length;
        PlaySound(_wallClimbClips[_wallClimbAudioIndex], 0.1f);
    }

    private void PlayLadderClimbSound()
    {
        if (_ladderClimbClips.Length == 0) return;
        _ladderClimbAudioIndex = (_ladderClimbAudioIndex + 1) % _ladderClimbClips.Length;
        PlaySound(_ladderClimbClips[_ladderClimbAudioIndex], 0.07f);
    }

    #endregion

    #region Helpers

    private void PlayRandomSound(IReadOnlyList<AudioClip> clips, float volume = 1, float pitch = 1)
    {
        if(clips.Count > 0)
            PlaySound(clips[UnityEngine.Random.Range(0, clips.Count)], volume, pitch);
    }

    private void PlaySound(AudioClip clip, float volume = 1, float pitch = 1)
    {
        if (clip == null) return;
        _source.pitch = pitch;
        _source.PlayOneShot(clip, volume);
    }

    #endregion

    #region Animation Keys

    private static readonly int GroundedKey = Animator.StringToHash("Grounded");
    private static readonly int IdleSpeedKey = Animator.StringToHash("IdleSpeed");
    private static readonly int JumpKey = Animator.StringToHash("Jump");

    #endregion
}
