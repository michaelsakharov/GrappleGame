using UnityEngine;

public class PlayerBounce : IPlayerInteractor
{
    [Header("NOTE: This adds to the player velocity, so it can be used to accumulate infinite velocity!!!")]
    public float bounceForce = 0.0f;

    public override void Interact(PlayerController player, ContactPoint2D hit)
    {
        player.AddForce(hit.normal * hit.normalImpulse * (1.0f + bounceForce));
    }

    public override void TriggerInteract(PlayerController player) { }

    public override void OnLeave(PlayerController player) { }
}
