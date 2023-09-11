﻿using UnityEngine;

public class PlayerWater : IPlayerInteractor
{
    public float waterDensity = 1.0f;

    public override void Interact(PlayerController player, ContactPoint2D hit)
    {
        player.dragAdders.Add(this, waterDensity);
    }

    public override void TriggerInteract(PlayerController player)
    {
        player.dragAdders.Add(this, waterDensity);
    }

    public override void OnLeave(PlayerController player)
    {
        player.dragAdders.Remove(this);
    }
}
