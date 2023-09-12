using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GrappleUnityAction : IGrappleInteractor
{
    public UnityEngine.Events.UnityEvent action;
    public UnityEngine.Events.UnityEvent leaveaction;
    public bool canAttach = true;
    public bool disconnect = false;

    public override bool Interact(PlayerController player, RaycastHit2D hit)
    {
        Debug.Log("Enter");
        action.Invoke();
        if(disconnect)
            player.SetState(PlayerController.PlayerState.Idle);
        return canAttach;
    }

    public override void OnLeave(PlayerController player)
    {
        Debug.Log("Leave");
        leaveaction.Invoke();
    }
}
