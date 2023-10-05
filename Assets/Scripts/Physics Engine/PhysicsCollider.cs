using UnityEngine;
using UnityEngine.UIElements;

[ExecuteAlways]
public abstract class PhysicsCollider : MonoBehaviour
{
    [ReadOnly] public dVector2 position;
    [ReadOnly] public dVector2 scale;
    [ReadOnly] public decimal rotation;

    public virtual void OnValidate()
    {
        // Must be static so that the since the Unity Transform uses Floats and is not deterministic
        this.gameObject.isStatic = true;
    }

#if UNITY_EDITOR
    public void Update()
    {
        if (!Application.isPlaying)
        {
            // Due to floating precision we store position as a decimal
            position = new((decimal)this.transform.position.x, (decimal)this.transform.position.y);
            scale = new((decimal)this.transform.localScale.x, (decimal)this.transform.localScale.y);
            rotation = (decimal)this.transform.rotation.eulerAngles.z;
        }
    }
#endif

    public abstract decimal GetDistance(dVector2 point);

    public dVector2 GetNormal(dVector2 p)
    {
        decimal EPSILON = 0.0001m;
        return dVector2.Normalize(new dVector2(
            GetDistance(new(p.X.value + EPSILON, p.Y.value)) - GetDistance(new(p.X.value - EPSILON, p.Y.value)),
            GetDistance(new(p.X.value, p.Y.value + EPSILON)) - GetDistance(new(p.X.value, p.Y.value - EPSILON))
        ));
    }

    public dVector2 SolveIntesection(dVector2 point, decimal radius)
    {
        // Calculate the distance from the point to the collider using the collider's SDF function
        decimal distance = GetDistance(point);

        // Calculate the penetration depth (how much the point has penetrated into the collider)
        decimal penetrationDepth = radius - distance;

        // If the point is outside the collider or just touching the edge, no intersection, return zero vector
        if (penetrationDepth >= 0)
        {
            // Calculate the direction vector that the point needs to move to no longer be intersecting the collider
            dVector2 normal = GetNormal(point);
            dVector2 movementVector = normal * penetrationDepth;

            return movementVector;
        }

        return new(0, 0);
    }
}
