using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GrappleUnityAction : IGrappleInteractor
{
    public UnityEngine.Events.UnityEvent action;
    public UnityEngine.Events.UnityEvent leaveaction;
    public bool canAttach = true;
    public bool disconnect = false;

    public override bool Interact(RaycastHit2D hit)
    {
        action.Invoke();
        if (disconnect)
            Grapple.Instance.DetatchGrapple();
        return canAttach;
    }

    public override void OnLeave()
    {
        leaveaction.Invoke();
    }
}
