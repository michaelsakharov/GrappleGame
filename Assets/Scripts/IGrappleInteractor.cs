using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IGrappleInteractor : MonoBehaviour
{
    public abstract bool Interact(RaycastHit2D hit);
    public abstract void OnLeave();
}
