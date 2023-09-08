using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GrappleBounce : IGrappleInteractor
{
    public float bounceForce = 0.3f;

    public override bool Interact(PlayerController player, RaycastHit2D hit)
    {
        player.GrappleDirection = Vector2.Reflect(player.GrappleDirection, hit.normal);
        player.GrappleDirection *= 1.0f + bounceForce;
        return false; // Return false to indicate that the grapple should not attatch to this object
    }

    // Since were bouncing the grapple, Leave happens when the grapple either hits a wall or the player releases the grapple button
    public override void OnLeave(PlayerController player) { }
}
