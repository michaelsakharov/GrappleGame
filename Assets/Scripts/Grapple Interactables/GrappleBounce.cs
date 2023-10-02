using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GrappleBounce : IGrappleInteractor
{
    public float bounceForce = 0.3f;

    public override bool Interact(PlayerController player, RaycastHit2D hit)
    {
        player.GrappleShootDirection = Vector2.Reflect(player.GrappleShootDirection, hit.normal);
        player.GrappleShootDirection *= 1.0f + bounceForce;
        return false; // Return false to indicate that the grapple should not attatch to this object
    }

    // Since were bouncing the grapple, Leave happens when the grapple either hits a wall or the player releases the grapple button
    public override void OnLeave(PlayerController player) { }
}
