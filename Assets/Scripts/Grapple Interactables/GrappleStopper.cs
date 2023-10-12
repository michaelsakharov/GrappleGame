using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GrappleStopper : IGrappleInteractor
{
    public override bool Interact(RaycastHit2D hit)
    {
        Grapple.Instance.DetatchGrapple();
        return false; // Can never attach to this object
    }

    public override void OnLeave() { }
}
