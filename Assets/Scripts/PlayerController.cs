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

    [Header("Movement")]
    [Header("Grounded")]
    public LayerMask groundLayer;
    public int groundedRayCount = 3;
    public float groundedRayLength = 0.1f;
    public float playerWidth = 0.5f;
    public float groundedRayHeightOffset = -0.39f;
    public float groundSpeed = 5f;
    public float groundDrag = 0.99f;
    [Header("Air")]
    public float aidSpeed = 5f;
    public float airDrag = 0.99f;
    public MMF_Player LandFeedback;


    bool isGrounded = false;
    Vector2 moveDir;
    Rigidbody2D rb;
    float noIsGroundedTimer = 0f;

    BoxCollider2D col;

    public enum PlayerState { Idle, Dead, Finished }
    PlayerState state = PlayerState.Idle;

    public Dictionary<object, float> gravityScalers = new Dictionary<object, float>();
    public Dictionary<object, float> dragAdders = new Dictionary<object, float>();

    public Rigidbody2D Rigidbody => rb;
    public PlayerState State => state;
    public bool IsGrounded => isGrounded;
    public Vector2 Velocity { get => rb.velocity; set => rb.velocity = value; }

    public event Action<Vector2> OnImpact;

    private void Awake()
    {
        Instance = this;

        col = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
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

        if (moveDir.x != 0 || moveDir.y != 0)
            GameManager.StartTimer();

        // check isGrounded
        isGrounded = CheckIsGrounded();

        if (state == PlayerState.Idle) // Is Idle
        {
            IsIdleLogic();
        }
        else if(state == PlayerState.Dead)
        {
            moveDir = Vector2.zero; // stop moving were ded :(
        }
        else if(state == PlayerState.Finished)
        {
            moveDir = Vector2.zero;
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
    }

    bool CheckIsGrounded()
    {
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
    }

    void IsHookedLogic()
    {
        // update grapple dist to be minimum
    }

    void IsShootingLogic() { }

    void IsShootingPhysicsLogic()
    {
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
