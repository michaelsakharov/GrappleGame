using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GrappleStopper : IGrappleInteractor
{
    public override bool Interact(PlayerController player, RaycastHit2D hit)
    {
        player.SetState(PlayerController.PlayerState.Idle);
        return false; // Can never attach to this object
    }

    public override void OnLeave(PlayerController player) { }
}
