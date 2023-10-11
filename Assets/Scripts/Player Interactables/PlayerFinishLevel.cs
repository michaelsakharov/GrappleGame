using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class PlayerFinishLevel : IPlayerInteractor
{
    public override void Interact(PlayerController player, ContactPoint2D hit)
    {
        Finish();
    }

    public override void TriggerInteract(PlayerController player)
    {
        Finish();
    }

    public void Finish()
    {
        PlayerController.Instance.FinishLevel();
    }

    public override void OnLeave(PlayerController player) { }
}
