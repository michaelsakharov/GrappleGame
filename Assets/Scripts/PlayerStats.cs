using UnityEngine;

[CreateAssetMenu]
public class PlayerStats : ScriptableObject
{
    // Setup
    [Header("Setup")] public ContactFilter2D GroundFilter;
    public ContactFilter2D LeftWallFilter;
    public ContactFilter2D RightWallFilter;
    public ContactFilter2D CeilingFilter;
    // Controller Setup
    [Header("Controller Setup"), Space] public float VerticalDeadZoneThreshold = 0.3f;
    public double HorizontalDeadZoneThreshold = 0.1f;

    // Movement
    [Header("Movement"), Space] public float BaseSpeed = 9;
    public float Acceleration = 50;
    public float Friction = 30;
    public float DirectionCorrectionMultiplier = 3f;
    public float AirAcceleration = 5;
    public float AirAccelerationUpMultiplier = 0.5f;
    public float AirDirectionCorrectionMultiplier = 1.5f;
    public float MaxWalkableSlope = 50;
    public float LandVelocityDeduction = 0.9f;

    // Jump
    [Header("Jump"), Space] public float BufferedJumpTime = 0.15f;
    public float CoyoteTime = 0.15f;
    public float JumpPower = 20;
    public float EndJumpEarlyExtraForceMultiplier = 3;
    public int MaxAirJumps = 1;

    // Walls
    [Header("Walls"), Space] public bool AllowWalls;
    public LayerMask WallJumpLayer;
    public float WallJumpInputLossTime = 0.5f;
    public bool RequireInputPush;
    public Vector2 WallJumpPower = new(6, 6);
    public bool WallJumpSpringy;
    public Vector2 WallSpringMultiplier = new(0.8f, 0.8f);
    public Vector2 WallJumpSpringMax = new(15, 15);
    [Tooltip("1 means 1 stored energy will dissipate a second")]
    public float WallSpringEnergyDissipation = 1f;
    public float WallSpringEnergyMin = 1f;
    public float WallFallAcceleration = 20;
    public float WallFallSpeed = 5;
    public float WallCoyoteTime = 0.3f;
}
