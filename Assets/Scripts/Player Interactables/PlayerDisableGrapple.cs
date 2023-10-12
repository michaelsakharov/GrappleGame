using UnityEngine;

public class PlayerDisableGrapple : IPlayerInteractor
{
    public override void Interact(PlayerController player, ContactPoint2D hit)
    {
        Grapple.Instance.canGrapple.Push(false);
    }

    public override void TriggerInteract(PlayerController player)
    {
        Grapple.Instance.canGrapple.Push(false);
    }

    public override void OnLeave(PlayerController player)
    {
        Grapple.Instance.canGrapple.Pop();
    }
}
