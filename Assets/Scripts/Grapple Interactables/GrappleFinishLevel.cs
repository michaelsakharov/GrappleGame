using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class GrappleFinishLevel : IGrappleInteractor
{
    public override bool Interact(PlayerController player, RaycastHit2D hit)
    {
        PlayerController.Instance.FinishLevel();

        return true;
    }

    public override void OnLeave(PlayerController player) { }
}
