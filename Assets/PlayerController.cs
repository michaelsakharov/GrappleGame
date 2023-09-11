using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{

    public static PlayerController Instance { get; private set; }

    [Header("Visual")]
    public GameObject visual;
    public GameObject deathEffect;
    public TMPro.TMP_Text levelTimerText;
    public GameObject levelFinishUI;
    public TMPro.TMP_Text finishLevelTimerText;

    [Header("Grapple")]
    public LayerMask grappleLayer;
    public Transform grappleIcon;
    public float grappleSpeed = 5f;
    public float grappleGravity = 2f;
    public float grappleGravityDelay = 0.25f;
    public float grappleDrag = 0.95f;
    public float grappleForce = 5f;


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


    bool isGrounded = false;
    Vector2 moveDir;
    Vector2 grapplePoint;
    Vector2 grappleShootDir;
    float grappleDist = -1;
    GameObject hookpointGO;
    DistanceJoint2D ropeJoint;
    LineRenderer line;
    Rigidbody2D rb;
    IGrappleInteractor activeInteractor;
    float noIsGroundedTimer = 0f;
    float shotTimer = 0f;

    float levelTimer = 0f;
    string nextlvl = "";

    public enum PlayerState { Idle, Hooked, Shooting, Dead, Finished }
    PlayerState state = PlayerState.Idle;

    public Dictionary<object, float> gravityScalers = new Dictionary<object, float>();
    public Dictionary<object, float> dragAdders = new Dictionary<object, float>();


    public Rigidbody2D Rigidbody => rb;
    public bool IsGrounded => isGrounded;
    public Vector2 GrapplePosition { get => grapplePoint; set => grapplePoint = value; }
    public Vector2 GrappleDirection { get => grappleShootDir; set => grappleShootDir = value; }
    public Vector2 Velocity { get => rb.velocity; set => rb.velocity = value; }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameObject linGO = new GameObject("Line");
        line = linGO.AddComponent<LineRenderer>();
        line.startWidth = 0.15f;
        line.endWidth = 0.1f;
        line.enabled = false;

        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (state != PlayerState.Finished)
            levelTimer += Time.deltaTime;
        levelTimerText.text = TimeSpan.FromSeconds(levelTimer).ToString(@"hh\:mm\:ss\:fff");
        finishLevelTimerText.text = TimeSpan.FromSeconds(levelTimer).ToString(@"hh\:mm\:ss\:fff");

        if (Input.GetKeyDown(KeyCode.R))
            RestartLevel();

        float hor = Input.GetAxis("Horizontal");
        float ver = Input.GetAxis("Vertical");
        moveDir = new Vector2(hor, ver).normalized;

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
            line.enabled = true;
            line.SetPosition(0, transform.position);
            line.SetPosition(1, grapplePoint);
            grappleIcon.transform.position = grapplePoint;

            // while shooting releasing the mouse will always cancel the shoot!
            if (Input.GetMouseButtonUp(0))
            {
                state = PlayerState.Idle;
                line.enabled = false;
            }
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
        rb.drag = isGrounded ? groundDrag : airDrag;
        rb.drag += dragAdders.Values.Sum();
        rb.AddForce(moveDir * (isGrounded ? groundSpeed : aidSpeed));

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
                    return true;
            }
        }
        else
        {
            noIsGroundedTimer -= Time.deltaTime;
        }
        return false;
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void LoadNextLevel()
    {
        SceneManager.LoadScene(nextlvl);
    }

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

    #region States

    void IsIdleLogic()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // start shooting
            grappleShootDir = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized * grappleSpeed;
            grapplePoint = this.transform.position;
            state = PlayerState.Shooting;
            shotTimer = 0f;
        }
    }

    void IsHookedLogic()
    {
        // update grapple dist to be minimum
        grappleDist = Mathf.Min(grappleDist, Vector2.Distance(transform.position, grapplePoint));
    }

    void IsShootingLogic()
    {
        shotTimer += Time.deltaTime;

        var prevPos = grapplePoint;
        if (shotTimer > grappleGravityDelay)
            grapplePoint.y -= grappleGravity * Time.deltaTime;
        grapplePoint += grappleShootDir * Time.deltaTime;
        grappleShootDir *= grappleDrag;
        // Distance travelled in 1 second
        var Vel = (grapplePoint - prevPos).magnitude / Time.deltaTime;
        // distance traveled in 1 physics step
        var dist = Vel * Time.fixedDeltaTime;

        // check if we hit something
        var hit = Physics2D.Raycast(grapplePoint, grappleShootDir, dist, grappleLayer);
        if (hit != default)
        {
            bool attach = true;

            // check if we hit an interactor
            if (hit.collider.TryGetComponent<IGrappleInteractor>(out var interactor))
            {
                attach = interactor.Interact(this, hit);
                activeInteractor = interactor;

                if(state != PlayerState.Shooting)
                {
                    // The Interactor has changed our state so we need to return
                    return;
                }
            }

            // if we hit something and we are allowed to attach
            if(attach)
            {
                state = PlayerState.Hooked;

                grapplePoint = hit.point;

                grappleDist = Vector2.Distance(transform.position, grapplePoint);
                hookpointGO = new GameObject("HookPoint");
                hookpointGO.transform.position = grapplePoint;
                var otherrb = hookpointGO.AddComponent<Rigidbody2D>();
                otherrb.isKinematic = true;
                ropeJoint = hookpointGO.AddComponent<DistanceJoint2D>();
                ropeJoint.distance = grappleDist;
                ropeJoint.connectedBody = this.rb;
                ropeJoint.maxDistanceOnly = true;
                if (activeInteractor != null)
                    activeInteractor.OnLeave(this);
                activeInteractor = null;
                DisableIsGrounded();
            }
        }
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

    public void Kill()
    {
        state = PlayerState.Dead;
        Invoke(nameof(RestartLevel), 2f);

        visual.SetActive(false);
        deathEffect.SetActive(true);

        // Stop all player motion
        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
    }

    public void FinishLevel(string nextLevel)
    {
        nextlvl = nextLevel;
        state = PlayerState.Finished;
        Invoke(nameof(LoadNextLevel), 2f);

        levelFinishUI.SetActive(true);

        // Stop all player motion
        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
    }

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
