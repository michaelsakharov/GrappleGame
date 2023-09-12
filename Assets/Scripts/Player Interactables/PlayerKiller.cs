using UnityEngine;

public class PlayerKiller : IPlayerInteractor
{
    public override void Interact(PlayerController player, ContactPoint2D hit)
    {
        player.Kill();
    }

    public override void TriggerInteract(PlayerController player)
    {
        player.Kill();
    }

    public override void OnLeave(PlayerController player) { }
}
