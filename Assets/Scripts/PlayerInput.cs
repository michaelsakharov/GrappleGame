using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public struct FrameInput
    {
        public Vector2 Move;
        public bool JumpDown;
        public bool JumpHeld;
        public bool GrappleDown;
    }

    public FrameInput Gather()
    {
        if(PlayerController.Instance.State == PlayerController.PlayerState.Dead)
            return new FrameInput();
        if(PlayerController.Instance.State == PlayerController.PlayerState.Finished)
            return new FrameInput();

        var input = new FrameInput
        {
            JumpDown = Input.GetKeyDown(KeyCode.Space),
            JumpHeld = Input.GetKey(KeyCode.Space),
            GrappleDown = Input.GetMouseButtonDown(0),
            Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
        };

        if (input.Move != Vector2.zero || input.JumpDown || input.JumpHeld || input.GrappleDown)
            GameManager.StartTimer();
        return input;
    }
}
