﻿using UnityEngine;

public abstract class IPlayerInteractor : MonoBehaviour
{
    public abstract void Interact(PlayerController player, ContactPoint2D contact);
    public abstract void OnLeave(PlayerController player);
}
