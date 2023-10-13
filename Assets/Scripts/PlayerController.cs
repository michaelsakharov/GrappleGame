using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlayerInput;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Range(0.5f, 20), Tooltip("How tall you are. This includes a collider and your step height.")]
    public float Height = 1.8f;

    [Range(0.1f, 0.9f), Tooltip("Affects your collider radius and is directly related to your height")]
    public float ChonkPercentage = .45f;

    public GameObject PlayerRenderer;
    public GameObject DeathEffect;

    private CapsuleCollider2D _collider;
    private ConstantForce2D _constantForce;
    private Rigidbody2D _rb;
    private PlayerInput _playerInput;

    public readonly Dictionary<object, float> GravityScalers = new();
    public readonly Dictionary<object, float> DragAdders = new();

    [field: SerializeField] public PlayerStats Stats { get; private set; }
    public enum JumpType { Jump, Coyote, AirJump, WallJump }
    public event Action<JumpType> Jumped;
    public event Action<bool, float> GroundedChanged;
    public event Action<bool> WallGrabChanged;
    public event Action<float> HeadBumb;

    public enum PlayerState { Normal, Dead, Finished }
    PlayerState state = PlayerState.Normal;

    public PlayerState State { get; private set; }
    
    public Vector2 Up { get; private set; }
    public Vector2 Right { get; private set; }
    public bool Grounded => _grounded;
    public Rigidbody2D Rigidbody => _rb;
    public Vector2 Input => _frameInput.Move;
    public Vector2 Velocity { get; private set; }
    public Vector2 PreviousVelocity { get; private set; }
    public Vector2 SmoothedVelocity { get; private set; }
    public int WallDirection { get; private set; }

    public bool SampleIsGrounded() => _rb.IsTouching(Stats.GroundFilter);

    public void AddForce(Vector2 force)
    {
        if(force.y > 0) DisableIsGrounded();
        _externalForceThisFrame += force;
    }

    void AddJumpForce(Vector2 force, bool resetVelocity = false)
    {
        if (resetVelocity) SetVelocity(Vector2.zero);
        _jumpForceThisFrame += force;
        DisableIsGrounded();
    }

    #region Loop

    private float _currentGravityScale = 1f;
    private bool _hasGrappledSinceLastGrounded = false;

    private void Awake()
    {
        Instance = this;
        if (!TryGetComponent(out _playerInput)) _playerInput = gameObject.AddComponent<PlayerInput>();
        if (!TryGetComponent(out _constantForce)) _constantForce = gameObject.AddComponent<ConstantForce2D>();
        SetupCharacter(true);
    }

    public void OnValidate() => SetupCharacter();

    public void Update()
    {
        GatherInput();

        if (!Grounded)
            _hasGrappledSinceLastGrounded |= Grapple.Instance.State == Grapple.GrappleState.Hooked;
        else
            _hasGrappledSinceLastGrounded = false;
    }

    public void FixedUpdate()
    {
        _rb.drag = Mathf.Max(DragAdders.Values.Sum(), 0.0f);

        float gravityScale = 1f;
        foreach (float f in GravityScalers.Values)
            gravityScale *= f;
        _rb.gravityScale = (_currentGravityScale * gravityScale) + GRAVITY_CONSTANT;

        SetFrameData();
        
        if (_rb.IsTouching(Stats.CeilingFilter))
            HeadBumb?.Invoke(SmoothedVelocity.y);

        CalculateCollisions();

        CalculateWalls();
        CalculateJump();

        Move();

        CleanFrameData();
    }

    #endregion

    #region Setup

    private bool _cachedQueryMode, _cachedQueryTriggers;
    private const float GRAVITY_CONSTANT = 0.001f;
    private const float GRAVITY_SCALE = 1 - GRAVITY_CONSTANT;
    private Vector2 _standingColliderSize;

    private void SetupCharacter(bool snapToGround = false)
    {
        _collider = GetComponent<CapsuleCollider2D>();
        _rb = GetComponent<Rigidbody2D>();


        _cachedQueryMode = Physics2D.queriesStartInColliders;

        _standingColliderSize = new Vector2((Height * ChonkPercentage), Height);

        _collider.size = _standingColliderSize;

        if (snapToGround)
        {
            if (Stats == null) return; // This sometimes gets hit in editor, not sure why
            Physics2D.queriesStartInColliders = true;
            // Snap to ground take col size into account
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 2f, Stats.GroundFilter.layerMask);
            if (hit)
                transform.position = hit.point + new Vector2(0, Height / 2);
            Physics2D.queriesStartInColliders = _cachedQueryMode;
        }
    }

    #endregion

    #region Input

    private FrameInput _frameInput;

    private void GatherInput()
    {
        _frameInput = _playerInput.Gather();


        if (_frameInput.JumpDown)
        {
            _jumpToConsume = true;
            _timeJumpWasPressed = Time.time;
        }
    }

    #endregion

    #region Frame Data

    private bool _hasInputThisFrame;
    private Vector2 _trimmedFrameVelocity;
    private Vector2 _framePosition;
    private Vector2 _frameFootPosition;

    private void SetFrameData()
    {
        var rot = _rb.rotation * Mathf.Deg2Rad;
        Up = new Vector2(-Mathf.Sin(rot), Mathf.Cos(rot));
        Right = new Vector2(Up.y, -Up.x);
        _framePosition = _rb.position;
        _frameFootPosition = _framePosition - new Vector2(0, _collider.size.y / 2f);

        _hasInputThisFrame = _frameInput.Move.x != 0;

        PreviousVelocity = Velocity;
        SmoothedVelocity = Vector2.MoveTowards(SmoothedVelocity, Velocity, (Velocity.magnitude > SmoothedVelocity.magnitude ? 9999f : 10f) * Time.deltaTime);
        Velocity = _rb.velocity;


        _trimmedFrameVelocity = new Vector2(Velocity.x, 0);
    }

    private void CleanFrameData()
    {
        _jumpToConsume = false;
        _jumpForceThisFrame = Vector2.zero;
        _externalForceThisFrame = Vector2.zero;
    }

    #endregion

    #region Collisions

    private bool _grounded;
    private float _groundedDisableDelay = 0f;

    public void DisableIsGrounded(float delay = 0.1f)
    {
        _groundedDisableDelay = delay;
        if(_grounded) ToggleGrounded(false);
    }

    private void CalculateCollisions()
    {
        var isGroundedThisFrame = _rb.IsTouching(Stats.GroundFilter) && _groundedDisableDelay <= 0f;

        if (isGroundedThisFrame && !_grounded) ToggleGrounded(true);
        else if (!isGroundedThisFrame && _grounded) ToggleGrounded(false);

        _groundedDisableDelay -= Time.fixedDeltaTime;
        _groundedDisableDelay = Mathf.Max(0, _groundedDisableDelay);
    }

    private void ToggleGrounded(bool grounded)
    {
        _grounded = grounded;
        if (grounded)
        {
            // Just Landed deduct velocities
            _trimmedFrameVelocity *= (1.0f - Stats.LandVelocityDeduction);
            SetVelocity(_trimmedFrameVelocity * (1.0f - Stats.LandVelocityDeduction));
            GroundedChanged?.Invoke(true, SmoothedVelocity.y);
            _currentGravityScale = 0f;
            _constantForce.force = Vector2.zero;
            _coyoteUsable = true;
            _bufferedJumpUsable = true;

            ResetAirJumps();
        }
        else
        {
            GroundedChanged?.Invoke(false, 0);
            _timeLeftGrounded = Time.time;
            _currentGravityScale = GRAVITY_SCALE;
        }
    }

    #endregion

    #region Walls

    private const float WALL_REATTACH_COOLDOWN = 0.2f;

    private float _wallJumpInputNerfPoint;
    private int _wallDirectionForJump;
    private bool _isOnWall;
    private float _timeLeftWall;
    private float _currentWallSpeedVel;
    private float _canGrabWallAfter;
    private int _wallDirThisFrame;
    private float _wallJumpSpringEnergy;
    private int _wallDirectionForJumpCached;

    private bool HorizontalInputPressed => Mathf.Abs(_frameInput.Move.x) > Stats.HorizontalDeadZoneThreshold;
    private bool IsPushingAgainstWall => HorizontalInputPressed && (int)Mathf.Sign(_frameInput.Move.x) == _wallDirThisFrame;

    private void CalculateWalls()
    {
        _wallJumpSpringEnergy -= Stats.WallSpringEnergyDissipation * Time.fixedDeltaTime;
        _wallJumpSpringEnergy = Mathf.Max(0, _wallJumpSpringEnergy);

        if (!Stats.AllowWalls) return;

        bool leftWall = _rb.IsTouching(Stats.LeftWallFilter);
        bool rightWall = _rb.IsTouching(Stats.RightWallFilter);

        //var wallDir = _isOnWall ? WallDirection : _frameInput.Move.x;
        var wallDir = _isOnWall ? WallDirection : Mathf.Sign(PreviousVelocity.x);

        bool hasHitWall = false;
        if (wallDir < 0 && leftWall)
            hasHitWall = true;
        else if (wallDir > 0 && rightWall)
            hasHitWall = true;

        _wallDirThisFrame = hasHitWall ? (int)wallDir : 0;

        //if (!_isOnWall && ShouldStickToWall() && Time.time > _canGrabWallAfter && Velocity.y <= 0) ToggleOnWall(true);
        if (!_isOnWall && _wallDirThisFrame != 0 && !_grounded && Time.time > _canGrabWallAfter) ToggleOnWall(true);
        else if (_isOnWall && !ShouldStayOnWall()) ToggleOnWall(false);

        // If we're not grabbing a wall, let's check if we're against one for wall-jumping purposes
        if (!_isOnWall)
        {
            if (leftWall) _wallDirThisFrame = -1;
            else if (rightWall) _wallDirThisFrame = 1;
        }

        bool ShouldStayOnWall()
        {
            if (_wallDirThisFrame == 0 || _grounded) return false;
            if (HorizontalInputPressed && !IsPushingAgainstWall) return false; // If pushing away
            if (_wallJumpSpringEnergy <= 0) return false; // Ran out of energy
            return true;
            //return !Stats.RequireInputPush || (IsPushingAgainstWall);
        }
    }

    private void ToggleOnWall(bool on)
    {
        _isOnWall = on;

        if (on)
        {
            _wallJumpSpringEnergy = Mathf.Abs(PreviousVelocity.x);
            _wallJumpSpringEnergy = Mathf.Max(Stats.WallSpringEnergyMin, _wallJumpSpringEnergy);
            _bufferedJumpUsable = true;
            _wallJumpCoyoteUsable = true;
            WallDirection = _wallDirThisFrame;
            _wallDirectionForJumpCached = _wallDirThisFrame;
        }
        else
        {
            _timeLeftWall = Time.time;
            _canGrabWallAfter = Time.time + WALL_REATTACH_COOLDOWN;
            _rb.gravityScale = GRAVITY_SCALE;
            WallDirection = 0;
            if (Velocity.y > 0)
            {
                //AddJumpForce(new Vector2(0, Stats.WallPopForce), true);
            }

            ResetAirJumps();
        }

        WallGrabChanged?.Invoke(on);
    }

    #endregion

    #region Jump

    private const float JUMP_CLEARANCE_TIME = 0.25f;
    private bool IsWithinJumpClearance => _lastJumpExecutedTime + JUMP_CLEARANCE_TIME > Time.time;
    private float _lastJumpExecutedTime;
    private bool _bufferedJumpUsable;
    private bool _jumpToConsume;
    private float _timeJumpWasPressed;
    private Vector2 _jumpForceThisFrame;
    private bool _endedJumpEarly;
    private float _endedJumpForce;
    private int _airJumpsRemaining;
    private bool _wallJumpCoyoteUsable;
    private bool _coyoteUsable;
    private float _timeLeftGrounded;

    private bool HasBufferedJump => _bufferedJumpUsable && Time.time < _timeJumpWasPressed + Stats.BufferedJumpTime && !IsWithinJumpClearance;
    private bool CanUseCoyote => _coyoteUsable && !_grounded && Time.time < _timeLeftGrounded + Stats.CoyoteTime;
    private bool CanAirJump => !_grounded && _airJumpsRemaining > 0;
    //private bool CanWallJump => !_grounded && (_isOnWall || _wallDirThisFrame != 0) || (_wallJumpCoyoteUsable && Time.time < _timeLeftWall + Stats.WallCoyoteTime);
    private bool CanWallJump => !_grounded && (_isOnWall) || (_wallJumpCoyoteUsable && Time.time < _timeLeftWall + Stats.WallCoyoteTime);

    private void CalculateJump()
    {
        if (_jumpToConsume || HasBufferedJump)
        {
            if (CanWallJump) ExecuteJump(JumpType.WallJump);
            else if (_grounded) ExecuteJump(JumpType.Jump);
            else if (CanUseCoyote) ExecuteJump(JumpType.Coyote);
            else if (CanAirJump) ExecuteJump(JumpType.AirJump);
        }

        if(!_hasGrappledSinceLastGrounded)
            //if ((!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && Velocity.y > 0) || Velocity.y < 0) 
            if ((!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && Velocity.y > 0) && _frameInput.Move.y < Stats.VerticalDeadZoneThreshold) 
                _endedJumpEarly = true; // Early end detection
        _wallJumpInputNerfPoint = Mathf.MoveTowards(_wallJumpInputNerfPoint, 1, Time.fixedDeltaTime / Stats.WallJumpInputLossTime);
    }

    private void ExecuteJump(JumpType jumpType)
    {
        SetVelocity(_trimmedFrameVelocity);
        _endedJumpEarly = false;
        _bufferedJumpUsable = false;
        _lastJumpExecutedTime = Time.fixedDeltaTime;

        if (jumpType is JumpType.Jump or JumpType.Coyote)
        {
            _coyoteUsable = false;
            AddJumpForce(new Vector2(0, Stats.JumpPower));
        }
        else if (jumpType is JumpType.AirJump)
        {
            _airJumpsRemaining--;
            AddJumpForce(new Vector2(0, Stats.JumpPower));
        }
        else if (jumpType is JumpType.WallJump)
        {
            ToggleOnWall(false);

            _wallJumpCoyoteUsable = false;
            _wallJumpInputNerfPoint = 0;
            _wallDirectionForJump = _wallDirThisFrame;
            Vector2 power = Stats.WallJumpPower;
            if (Stats.WallJumpSpringy)
            {
                power = _wallJumpSpringEnergy * Stats.WallSpringMultiplier;
                power.x = Mathf.Max(power.x, Stats.WallJumpPower.x);
                power.y = Mathf.Max(power.y, Stats.WallJumpPower.y);
                power.x = Mathf.Min(power.x, Stats.WallJumpSpringMax.x);
                power.y = Mathf.Min(power.y, Stats.WallJumpSpringMax.y);
            }
            // We used up all our stored wall jump springy energy
            _wallJumpSpringEnergy = 0;

            AddJumpForce(new Vector2(-_wallDirectionForJumpCached, 1) * power);
            //if (_isOnWall || IsPushingAgainstWall)
            //    AddJumpForce(new Vector2(-_wallDirThisFrame, 1) * power);
            //else
            //    AddJumpForce(new Vector2(-_wallDirThisFrame, 1) * power);
        }

        Jumped?.Invoke(jumpType);
    }

    private void ResetAirJumps() => _airJumpsRemaining = Stats.MaxAirJumps;

    #endregion

    #region Move

    private Vector2 _frameSpeedModifier, _currentFrameSpeedModifier = Vector2.one;
    private Vector2 _externalForceThisFrame;

    private void Move()
    {
        if(state == PlayerState.Dead)
            return; // Don't move if dead

        // Dont need to handle finished state, thats done in PlayerInput, it just disables input

        _rb.AddForce(_externalForceThisFrame);

        if (Grapple.Instance.State == Grapple.GrappleState.Hooked)
        {
            _endedJumpEarly = false;
            return; // Don't move if grappled, Just let physics do its thing
        }

        if (_jumpForceThisFrame != Vector2.zero)
        {
            _rb.AddForce(_jumpForceThisFrame * _rb.mass, ForceMode2D.Impulse);
            // we prioritize all jump force for this frame
            // this way we can that frame perfect instant jump feel
            return;
        }

        if (_isOnWall)
        {
            _constantForce.force = Vector2.zero;

            //float wallVelocity = Mathf.MoveTowards(Mathf.Min(Velocity.y, 0), -Stats.WallFallSpeed, Stats.WallFallAcceleration * Time.fixedDeltaTime);
            if (Velocity.y <= 0)
            {
                float wallVelocity = Mathf.MoveTowards(Mathf.Min(Velocity.y, 0), -Stats.WallFallSpeed, Stats.WallFallAcceleration * Time.fixedDeltaTime);
                SetVelocity(new Vector2(_rb.velocity.x, wallVelocity));
            }
            return;
        }

        var extraForce = Vector2.zero;
        if (!_grounded && _endedJumpEarly && Velocity.y > 0)
            extraForce.y -= Stats.EndJumpEarlyExtraForceMultiplier;
        _constantForce.force = extraForce * _rb.mass;

        if (!_grounded)
        {
            var move = _frameInput.Move;
            if (move.y > 0) move.y *= Stats.AirAccelerationUpMultiplier;
            if (move.magnitude > 1) move.Normalize();

            // Quicker direction change
            if (Vector3.Dot(_trimmedFrameVelocity, new Vector2(_frameInput.Move.x, 0)) < 0) 
                move.x *= Stats.AirDirectionCorrectionMultiplier;

            _rb.AddForce(Stats.AirAcceleration * move);
            // normal movement is disabled when in the air
            return;
        }

        var targetSpeed = _hasInputThisFrame ? Stats.BaseSpeed : 0;

        var step = _hasInputThisFrame ? Stats.Acceleration : Stats.Friction;

        var xDir = (_hasInputThisFrame ? new Vector2(_frameInput.Move.x, 0) : Velocity.normalized);

        // Quicker direction change
        if (Vector3.Dot(_trimmedFrameVelocity, new Vector2(_frameInput.Move.x, 0)) < 0) 
            step *= Stats.DirectionCorrectionMultiplier;

        Vector2 newVelocity;
        step *= Time.fixedDeltaTime;

        var speed = Mathf.MoveTowards(Velocity.magnitude, targetSpeed, step);

        // Blend the two approaches
        var targetVelocity = xDir * speed;

        // Calculate the new speed based on the current and target speeds
        // Accurate but abrupt
        var newSpeed = Mathf.MoveTowards(Velocity.magnitude, targetVelocity.magnitude, step);
        //newVelocity = targetVelocity.normalized * newSpeed;
        // Smooth but potentially inaccurate
        var smoothed = Vector2.MoveTowards(Velocity, targetVelocity, step);
        //newVelocity = smoothed;
        // The literal middle ground between the two, attempting to get the best of both worlds
        newVelocity = Vector2.Lerp(targetVelocity.normalized * newSpeed, smoothed, 0.8f);

        SetVelocity(newVelocity * _currentFrameSpeedModifier);
    }

    private void SetVelocity(Vector2 newVel)
    {
        _rb.velocity = newVel;
        Velocity = newVel;
    }

    #endregion

    #region External Triggers

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var interactor = collision.gameObject.GetComponents<IPlayerInteractor>();
        if (interactor != null)
            foreach (var i in interactor)
                i.Interact(this, collision.contacts.FirstOrDefault());
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        var interactor = collision.gameObject.GetComponents<IPlayerInteractor>();
        if (interactor != null)
            foreach (var i in interactor)
                i.OnLeave(this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var interactor = collision.gameObject.GetComponents<IPlayerInteractor>();
        if (interactor != null)
            foreach (var i in interactor)
                i.TriggerInteract(this);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var interactor = collision.gameObject.GetComponents<IPlayerInteractor>();
        if (interactor != null)
            foreach (var i in interactor)
                i.OnLeave(this);
    }

    #endregion

    #region Public Methods

    void RestartLevel() => GameManager.RestartLevel();
    void GM_FinishLevel() => GameManager.FinishLevel();

    public void Kill()
    {
        GameManager.StopTimer();

        state = PlayerState.Dead;
        Invoke(nameof(RestartLevel), 1f);

        if(PlayerRenderer != null) PlayerRenderer.SetActive(false);
        if (DeathEffect != null) DeathEffect.SetActive(true);

        // Stop all player motion
        _rb.isKinematic = true;
        _rb.velocity = Vector2.zero;
    }

    public void FinishLevel()
    {
        GameManager.StopTimer();

        state = PlayerState.Finished;
        Invoke(nameof(GM_FinishLevel), 3f);

        GameHud.Instance.levelFinishUI.SetActive(true);

        // Stop all player motion
        //_rb.isKinematic = true;
        //_rb.velocity = Vector2.zero;
    }

    public void ToggleCollider() => _collider.enabled = !_collider.enabled;

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        var pos = (Vector2)transform.position - new Vector2(0, _collider.size.y/2f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(pos + Vector2.up * (Height / 2), new Vector2(_standingColliderSize.x, Height));
    }

    #endregion

}
