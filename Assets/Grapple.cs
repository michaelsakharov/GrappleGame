using MoreMountains.Feedbacks;
using System.Collections.Generic;
using UnityEngine;
using static PlayerController;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody2D))]
public class Grapple : MonoBehaviour
{
    public static Grapple Instance { get; private set; }

    public Vector2 GrapplePosition { get => grapplePoint; set => grapplePoint = value; }
    public Vector2 GrapplePointVelocity { get => grapplePointVelocity; set => grapplePointVelocity = value; }
    public Vector2 GrappleDirection => (grapplePoint - (Vector2)transform.position).normalized;

    public LayerMask grappleLayer;
    public Transform grappleIcon;
    public float grappleSpeed = 75f;
    public float grappleGravity = 9f;
    public float grappleGravityDelay = 0.1f;
    public float grappleDrag = 10f;
    public float grappleDragDelay = 0.075f;
    public float grappleForce = 25f;
    public MMF_Player GrappleLaunchFeedback;
    public MMF_Player GrappleHitFeedback;
    public Material ropeMaterial;

    LineRenderer line;
    DistanceJoint2D ropeJoint;

    IGrappleInteractor activeInteractor;

    Vector2 grapplePoint;
    Vector2 prevGrapplePoint;
    Vector2 grapplePointVelocity;
    Vector2 grappleHitNormal;
    float grappleDist = -1;
    GameObject hookpointGO;
    float shotTimer = 0f;
    internal Stack<bool> canGrapple = new Stack<bool>();

    public enum GrappleState { None, Shooting, Hooked }
    GrappleState state = GrappleState.None;

    public LineRenderer LineRenderer => line;
    public GrappleState State => state;

    private void Awake()
    {
        Instance = this;

        GameObject linGO = new GameObject("Line");
        line = linGO.AddComponent<LineRenderer>();
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {

        if (state == GrappleState.None) // Is Idle
        {
            if (Input.GetMouseButtonDown(0))
                if (canGrapple.Count == 0 || canGrapple.Peek())
                {
                    // start shooting
                    grapplePointVelocity = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized * grappleSpeed;
                    grapplePointVelocity += PlayerController.Instance.Velocity; // Add our velocity to it
                    grapplePoint = this.transform.position;
                    prevGrapplePoint = grapplePoint;
                    state = GrappleState.Shooting;
                    shotTimer = 0f;
                    GrappleLaunchFeedback?.PlayFeedbacks();
                }
        }
        else if (state == GrappleState.Shooting) // Is Shooting
        {
            // Nothing happens here
        }
        else if (state == GrappleState.Hooked) // Is Hooked
        {
            PlayerController.Instance.DisableIsGrounded();
            grappleDist = Mathf.Min(grappleDist, Vector2.Distance(transform.position, grapplePoint));
        }

        if (state == GrappleState.Shooting || state == GrappleState.Hooked)
        {
            // if cant grapple
            if (canGrapple.Count > 0 && !canGrapple.Peek())
                DetatchGrapple();

            line.enabled = true;
            line.SetPosition(1, transform.position);
            line.SetPosition(0, grapplePoint);
            line.material = ropeMaterial;
            line.textureMode = LineTextureMode.Tile;

            Vector3 interpolatedPosition = Vector3.Lerp(prevGrapplePoint, GrapplePosition, (float)(Time.timeAsDouble - Time.fixedTimeAsDouble) / Time.fixedDeltaTime);
            grappleIcon.transform.position = interpolatedPosition;

            // while shooting releasing the mouse will always cancel the shoot!
            if (Input.GetMouseButtonUp(0))
                DetatchGrapple();
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
                activeInteractor.OnLeave();
            activeInteractor = null;
        }
    }

    private void FixedUpdate()
    {
        prevGrapplePoint = grapplePoint; // Set before update and always set
        if (state == GrappleState.Shooting) // Is Shooting
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
                    attach = interactor.Interact(hit);
                    if (activeInteractor != null) // If we were attached to something else leave it
                        activeInteractor.OnLeave();
                    activeInteractor = interactor;

                    if (state != GrappleState.Shooting)
                    {
                        // The Interactor has changed our state so we need to return
                        return;
                    }
                }

                // if we hit something and we are allowed to attach
                if (attach)
                {
                    state = GrappleState.Hooked;

                    grapplePoint = hit.point;
                    grappleHitNormal = hit.normal;

                    grappleDist = Vector2.Distance(transform.position, grapplePoint);
                    hookpointGO = new GameObject("HookPoint");
                    hookpointGO.transform.position = grapplePoint;
                    var otherrb = hookpointGO.AddComponent<Rigidbody2D>();
                    otherrb.isKinematic = true;
                    ropeJoint = hookpointGO.AddComponent<DistanceJoint2D>();
                    ropeJoint.distance = grappleDist;
                    ropeJoint.connectedBody = PlayerController.Instance.Rigidbody;
                    ropeJoint.maxDistanceOnly = true;
                    PlayerController.Instance.DisableIsGrounded();

                    GrappleHitFeedback?.PlayFeedbacks();
                }
            }

            if (shotTimer >= grappleDragDelay)
                grapplePointVelocity *= 1f - (grappleDrag * Time.fixedDeltaTime);
        }
        else if (state == GrappleState.Hooked) // Is Hooked
        {
            // Grapple force
            PlayerController.Instance.AddForce((grapplePoint - (Vector2)transform.position).normalized * grappleForce);
            // apply a Rope force to stop the player exceeding rope length
            ropeJoint.distance = grappleDist;

            // Cant be grounded while hooked
            PlayerController.Instance.DisableIsGrounded(0.05f);
        }
    }

    public void DetatchGrapple()
    {
        if (State == GrappleState.None) return;
        state = GrappleState.None;
        line.enabled = false;
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(grapplePoint, 0.1f);
    }
}
