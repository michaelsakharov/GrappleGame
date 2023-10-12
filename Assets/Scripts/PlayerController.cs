using MoreMountains.Feedbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Visual")]
    public GameObject visual;
    public GameObject deathEffect;

    [Header("Grapple")]
    public LayerMask grappleLayer;
    public Transform grappleIcon;
    public float grappleSpeed = 5f;
    public float grappleGravity = 2f;
    public float grappleGravityDelay = 0.25f;
    public float grappleDrag = 0.95f;
    public float grappleDragDelay = 0.25f;
    public float grappleForce = 5f;
    public MMF_Player GrappleLaunchFeedback;
    public MMF_Player GrappleHitFeedback;
    public Material ropeMaterial;


    [Header("Movement")]
    [Header("Grounded")]
    public LayerMask groundLayer;
    public int groundedRayCount = 3;
    public float groundedRayLength = 0.1f;
    public float playerWidth = 0.5f;
    public float groundedRayHeightOffset = -0.39f;
    public bool canBeGroundedWhileHooked = false;
    public float groundSpeed = 5f;
    public float groundDrag = 0.99f;
    [Header("Air")]
    public float aidSpeed = 5f;
    public float airDrag = 0.99f;
    public MMF_Player LandFeedback;


    bool isGrounded = false;
    Vector2 moveDir;
    Vector2 grapplePoint;
    Vector2 prevGrapplePoint;
    Vector2 grapplePointVelocity;
    Vector2 grappleHitNormal;
    float grappleDist = -1;
    GameObject hookpointGO;
    DistanceJoint2D ropeJoint;
    internal LineRenderer line;
    Rigidbody2D rb;
    IGrappleInteractor activeInteractor;
    float noIsGroundedTimer = 0f;
    float shotTimer = 0f;

    BoxCollider2D col;

    public enum PlayerState { Idle, Hooked, Shooting, Dead, Finished }
    PlayerState state = PlayerState.Idle;

    public Dictionary<object, float> gravityScalers = new Dictionary<object, float>();
    public Dictionary<object, float> dragAdders = new Dictionary<object, float>();
    public Stack<bool> canGrapple = new Stack<bool>();

    public Rigidbody2D Rigidbody => rb;
    public PlayerState State => state;
    public bool IsGrounded => isGrounded;
    public bool IsGrappling => state == PlayerState.Hooked || state == PlayerState.Shooting;
    public Vector2 GrapplePosition { get => grapplePoint; set => grapplePoint = value; }
    public Vector2 GrapplePointVelocity { get => grapplePointVelocity; set => grapplePointVelocity = value; }
    public Vector2 GrappleDirection => (grapplePoint - (Vector2)transform.position).normalized;
    public Vector2 Velocity { get => rb.velocity; set => rb.velocity = value; }

    public event Action<Vector2> OnImpact;

    private void Awake()
    {
        Instance = this;

        col = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        GameObject linGO = new GameObject("Line");
        line = linGO.AddComponent<LineRenderer>();
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.enabled = false;

        rb = GetComponent<Rigidbody2D>();

        // TODO: Ensure Tilemap has finished loading before doing this
        // Snap to ground take col size into account
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 2f, groundLayer);
        if (hit)
        {
            transform.position = hit.point + new Vector2(0, col.size.y / 2);
        }
    }

    void Update()
    {
        moveDir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // Snap Movement once past Threshold
        const float Threshold = 0.1f;
        moveDir.x = Mathf.Abs(moveDir.x) < Threshold ? 0 : Mathf.Sign(moveDir.x);
        moveDir.y = Mathf.Abs(moveDir.y) < Threshold ? 0 : Mathf.Sign(moveDir.y);

        if (moveDir.x != 0 || moveDir.y != 0 || state == PlayerState.Shooting || state == PlayerState.Hooked)
            GameManager.StartTimer();

        // check isGrounded
        isGrounded = CheckIsGrounded();

        if (state == PlayerState.Idle) // Is Idle
        {
            IsIdleLogic();
        }
        else if (state == PlayerState.Shooting) // Is Shooting
        {
            IsShootingLogic();
        }
        else if (state == PlayerState.Hooked) // Is Hooked
        {
            IsHookedLogic();
        }
        else if(state == PlayerState.Dead)
        {
            moveDir = Vector2.zero; // stop moving were ded :(
        }
        else if(state == PlayerState.Finished)
        {
            moveDir = Vector2.zero;
        }

        if (state == PlayerState.Shooting || state == PlayerState.Hooked)
        {
            // if cant grapple
            if (canGrapple.Count > 0 && !canGrapple.Peek())
            {
                state = PlayerState.Idle;
                line.enabled = false;
            }

            line.enabled = true;
            line.SetPosition(1, transform.position);
            line.SetPosition(0, grapplePoint);
            line.material = ropeMaterial;
            line.textureMode = LineTextureMode.Tile;

            Vector3 interpolatedPosition = Vector3.Lerp(prevGrapplePoint, GrapplePosition, (float)(Time.timeAsDouble - Time.fixedTimeAsDouble) / Time.fixedDeltaTime);
            grappleIcon.transform.position = interpolatedPosition;

            // while shooting releasing the mouse will always cancel the shoot!
            if (Input.GetMouseButtonUp(0))
                DetachGrapple();
        }
        else
        {
            // Were not shooting or hooked so we can reset the entire state to idle
            line.enabled = false;
            grappleIcon.transform.position = new Vector3(9999f, 9999f, 0f);

            // make sure anything we created is destroyed
            if (ropeJoint != null) Destroy(ropeJoint);
            if (hookpointGO != null) Destroy(hookpointGO);

            // if we has an grapple interactor we need to notify the interactor that we have left
            if (activeInteractor != null)
                activeInteractor.OnLeave(this);
            activeInteractor = null;
        }

        float gravityScale = 1f;
        foreach (float f in gravityScalers.Values)
            gravityScale *= f;
        rb.gravityScale = gravityScale;
    }

    void FixedUpdate()
    {
        float drag = isGrounded ? groundDrag : airDrag;
        drag += dragAdders.Values.Sum();
        rb.drag = Mathf.Max(drag, 0.0f);
        rb.AddForce(moveDir * (isGrounded ? groundSpeed : aidSpeed));

        prevGrapplePoint = grapplePoint; // Set before update and always set
        if (state == PlayerState.Shooting) // Is Shooting
        {
            IsShootingPhysicsLogic();
        }

        if (state == PlayerState.Hooked) // Is Hooked
        {
            // Grapple force
            rb.AddForce((grapplePoint - (Vector2)transform.position).normalized * grappleForce);
            // apply a Rope force to stop the player exceeding rope length
            ropeJoint.distance = grappleDist;
        }
    }

    bool CheckIsGrounded()
    {
        if (state == PlayerState.Hooked && !canBeGroundedWhileHooked)
        {
            DisableIsGrounded(); // Add a short timer to bring back isGrounded
            return false;
        }

        if (noIsGroundedTimer > 0f)
        {
            Vector2 start = transform.position + new Vector3(-(playerWidth / 2f), groundedRayHeightOffset, 0f);
            for (int i = 0; i < groundedRayCount; i++)
            {
                Vector2 rayOrigin = start + new Vector2(playerWidth / (groundedRayCount - 1) * i, 0f);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundedRayLength, groundLayer);
                if (hit.collider != null)
                {
                    if(isGrounded == false)
                    {
                        // we just landed
                        LandFeedback?.PlayFeedbacks();
                    }
                    return true;
                }
            }
        }
        else
        {
            noIsGroundedTimer -= Time.deltaTime;
        }
        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var interactor = collision.gameObject.GetComponents<IPlayerInteractor>();
        if (interactor != null)
            foreach (var i in interactor)
                i.Interact(this, collision.contacts.FirstOrDefault());

        if (!IsGrounded)
            if (collision.relativeVelocity.magnitude > 0.5)
                OnImpact?.Invoke(collision.relativeVelocity);
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

    #region States

    void IsIdleLogic()
    {
        if (canGrapple.Count == 0 || canGrapple.Peek())
        {
            if (Input.GetMouseButtonDown(0))
            {
                // start shooting
                grapplePointVelocity = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized * grappleSpeed;
                grapplePointVelocity += rb.velocity; // Add our velocity to it
                grapplePoint = this.transform.position;
                prevGrapplePoint = grapplePoint;
                state = PlayerState.Shooting;
                shotTimer = 0f; 
                GrappleLaunchFeedback?.PlayFeedbacks();
            }
        }
    }

    void IsHookedLogic()
    {
        // update grapple dist to be minimum
        grappleDist = Mathf.Min(grappleDist, Vector2.Distance(transform.position, grapplePoint));
    }

    void IsShootingLogic() { }

    void IsShootingPhysicsLogic()
    {
        shotTimer += Time.fixedDeltaTime;
        var before = grapplePoint;
        if (shotTimer >= grappleGravityDelay)
            grapplePoint.y -= grappleGravity * Time.fixedDeltaTime;
        grapplePoint += grapplePointVelocity * Time.fixedDeltaTime;

        // check if we hit something
        //var hit = Physics2D.Raycast(grapplePoint, grapplePointVelocity.normalized, (grapplePointVelocity * Time.fixedDeltaTime).magnitude, grappleLayer);
        // Raycast from Before to GrapplePoint
        var hit = Physics2D.Raycast(before, grapplePoint - before, (grapplePoint - before).magnitude, grappleLayer);
        if (hit != default)
        {
            bool attach = true;

            // check if we hit an interactor
            if (hit.collider.TryGetComponent<IGrappleInteractor>(out var interactor))
            {
                attach = interactor.Interact(this, hit);
                if (activeInteractor != null) // If we were attached to something else leave it
                    activeInteractor.OnLeave(this);
                activeInteractor = interactor;

                if (state != PlayerState.Shooting)
                {
                    // The Interactor has changed our state so we need to return
                    return;
                }
            }

            // if we hit something and we are allowed to attach
            if (attach)
            {
                state = PlayerState.Hooked;

                grapplePoint = hit.point;
                grappleHitNormal = hit.normal;

                grappleDist = Vector2.Distance(transform.position, grapplePoint);
                hookpointGO = new GameObject("HookPoint");
                hookpointGO.transform.position = grapplePoint;
                var otherrb = hookpointGO.AddComponent<Rigidbody2D>();
                otherrb.isKinematic = true;
                ropeJoint = hookpointGO.AddComponent<DistanceJoint2D>();
                ropeJoint.distance = grappleDist;
                ropeJoint.connectedBody = this.rb;
                ropeJoint.maxDistanceOnly = true;
                DisableIsGrounded();

                GrappleHitFeedback?.PlayFeedbacks();
            }
        }

        if (shotTimer >= grappleDragDelay)
            grapplePointVelocity *= 1f - (grappleDrag * Time.fixedDeltaTime);
    }

    #endregion

    #region Public Methods

    public void DisableIsGrounded(float delay = 0.1f)
    {
        noIsGroundedTimer = delay;
    }

    public void SetState(PlayerState idle)
    {
        state = idle;
    }

    internal void DetachGrapple()
    {
        if (!IsGrappling) return;
        state = PlayerState.Idle;
        line.enabled = false;
    }


    void RestartLevel() => GameManager.RestartLevel();
    void GM_FinishLevel() => GameManager.FinishLevel();

    public void Kill()
    {
        GameManager.StopTimer();

        state = PlayerState.Dead;
        Invoke(nameof(RestartLevel), 1f);

        visual.SetActive(false);
        deathEffect.SetActive(true);

        // Stop all player motion
        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
    }

    public void FinishLevel()
    {
        GameManager.StopTimer();

        state = PlayerState.Finished;
        Invoke(nameof(GM_FinishLevel), 3f);

        GameHud.Instance.levelFinishUI.SetActive(true);

        // Stop all player motion
        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
    }

    public void ToggleCollider() => col.enabled = !col.enabled;

    #endregion

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(grapplePoint, 0.1f);
        // visualize isGrounded Ray
        Gizmos.color = Color.green;
        Vector2 start = transform.position + new Vector3(-(playerWidth / 2f), groundedRayHeightOffset, 0f);
        for (int i = 0; i < groundedRayCount; i++)
        {
            Vector2 rayOrigin = start + new Vector2(playerWidth / (groundedRayCount - 1) * i, 0f);
            Gizmos.DrawRay(rayOrigin, Vector2.down * groundedRayLength);
        }
    }
}
