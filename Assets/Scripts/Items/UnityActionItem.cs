using UnityEngine;
using UnityEngine.Events;

public class UnityActionItem : ItemObject
{
    public bool aiming = false;
    public bool aimmirroring = false;

    public UnityAction action;

    public override bool DoAim() => aiming;

    public override bool DoAimMirror() => aimmirroring;

    public override void ItemUpdate()
    {
        if (Input.GetButton("UseItem"))
            action?.Invoke();
    }
}
