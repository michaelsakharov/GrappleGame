using System;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteAlways]
public class PhysicsBody : MonoBehaviour
{
    public UnityDecimal radius;

    // Uses Verlet integration so we dont really store velocity but rather the last position and the current position
    // the velocity is calculated by the delta between the last position and the current position
    [HideInInspector] public dVector2 Position;
    [HideInInspector] public dVector2 LastPosition;
    // Acceleration is used to apply external forces to the body (gravity, wind, etc)
    [HideInInspector] public dVector2 Acceleration;

    public event Action<decimal> OnCollision; // first decimal is the Impact Force

    public void OnValidate()
    {
        radius = Math.Max(radius, 0.0m);
    }

    // Ok so this is a bit of a hack
    // THe physics engine is deterministic, unity positions... are... well not :(
    // so we store the position as a decimal when in editor and not playing
    // this should always be the same as the unity position when editing
#if UNITY_EDITOR
    public void Update()
    {
        if (!Application.isPlaying)
        {
            // Due to floating precision we store position as a decimal
            Position = new((decimal)this.transform.position.x, (decimal)this.transform.position.y);
            Debug.Log(Position.X + ":" + Position.Y);
            LastPosition = Position;
        }
    }
#endif

    public void UpdateBody()
    {
        var displacement = Position - LastPosition;
        LastPosition = Position;
        Position += displacement + Acceleration * (PhysicsManager.stepDt * PhysicsManager.stepDt);
        this.transform.position = new Vector3((float)Position.X.value, (float)Position.Y.value, 0.0f);
        Acceleration = new(0.0m, 0.0m);
    }

    public void Accelerate(dVector2 v) => Acceleration += v;
    public void SetVelocity(dVector2 v) => LastPosition = Position - (v * PhysicsManager.stepDt);
    public void AddVelocity(dVector2 v) => LastPosition -= v * PhysicsManager.stepDt;
    public dVector2 GetVelocity() => (Position - LastPosition) / PhysicsManager.stepDt;

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, (float)radius.value);
    }
}
