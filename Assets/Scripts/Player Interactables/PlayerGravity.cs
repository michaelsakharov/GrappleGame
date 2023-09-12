using UnityEngine;

public class PlayerGravity : IPlayerInteractor
{
    public float gravityScale = 1.0f;

    public override void Interact(PlayerController player, ContactPoint2D hit)
    {
        player.gravityScalers.Add(this, gravityScale);
    }

    public override void TriggerInteract(PlayerController player)
    {
        player.gravityScalers.Add(this, gravityScale);
    }

    public override void OnLeave(PlayerController player)
    {
        player.gravityScalers.Remove(this);
    }
}
