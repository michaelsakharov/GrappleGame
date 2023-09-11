using UnityEngine;

public class PlayerDisableGrapple : IPlayerInteractor
{
    public override void Interact(PlayerController player, ContactPoint2D hit)
    {
        player.canGrapple.Push(false);
    }

    public override void TriggerInteract(PlayerController player)
    {
        player.canGrapple.Push(false);
    }

    public override void OnLeave(PlayerController player)
    {
        player.canGrapple.Pop();
    }
}
