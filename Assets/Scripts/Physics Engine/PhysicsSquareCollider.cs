using System;
using UnityEngine;

public class PhysicsSquareCollider : PhysicsCollider
{
    public dVector2 size = new();

    public override void OnValidate()
    {
        base.OnValidate();
        size.X.value = Math.Max(size.X.value, 0.0m);
        size.Y.value = Math.Max(size.Y.value, 0.0m);
    }

    public override decimal GetDistance(dVector2 worldPoint)
    {
        // Square sdf
        // make relative to center
        worldPoint -= position;
        dVector2 d = dVector2.Abs(worldPoint) - ((scale / 2m) * size);
        return dVector2.Length(dVector2.Max(d, new(0.0m, 0.0m))) + Math.Min(Math.Max(d.X.value, d.Y.value), 0.0m);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        dVector2 s = scale * size;
        Gizmos.DrawWireCube(transform.position, new Vector3((float)s.X.value, (float)s.Y.value, 0.0f));
    }
}
