using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PhysicsManager : MonoBehaviour
{
    public static PhysicsManager Instance;
    public static void AddBody(PhysicsBody body) => Instance.physicsBodies.Add(body);

    private const uint subSteps = 4;
    private const decimal frameDt = 1.0m / 144.0m;
    public const decimal stepDt = frameDt / subSteps;

    public UnityDecimal gravity = new(9.8m);
    [ReadOnly] public decimal time = 0.0m;

    public List<PhysicsBody> physicsBodies = new();
    public List<PhysicsCollider> colliders = new();

    public static event Action<decimal> OnStep;

    void Awake()
    {
        Instance = this;

        physicsBodies = FindObjectsOfType<PhysicsBody>().ToList();

        colliders = FindObjectsOfType<PhysicsCollider>().ToList();
    }

    void Update()
    {
        // TODO: Potential point of non-determinism Casting a Float type to a decimal type
        // might not be deterministic across different platforms
        // though this should be miniscule in terms of impact
        // worse case a step might occur late, but since all steps are the same length
        // it should not be a problem for the simulation
        time += (decimal)Time.deltaTime;

        while(time > frameDt)
        {
            for (uint i = subSteps; i > 0; --i)
            {
                // First apply gravity
                dVector2 grav = new(0.0m, gravity.value);
                foreach (var obj in physicsBodies)
                    obj.Accelerate(grav);

                CheckCollisions();

                OnStep?.Invoke(stepDt);

                // Move the body using the newly calculated data
                // This must be last so that bodies always have the latest data
                // And nothing is delayed by a frame
                foreach (var obj in physicsBodies)
                    obj.UpdateBody();
            }
            time -= frameDt;
        }
    }

    private void CheckCollisions()
    {
        const decimal responseCoef = 0.75m;
        ulong objectsCount = (ulong)physicsBodies.Count;
        for (ulong i = 0; i < objectsCount; ++i)
        {
            PhysicsBody a = physicsBodies[(int)i];
            for (ulong k = i + 1; k < objectsCount; ++k)
            {
                PhysicsBody b = physicsBodies[(int)k];
                dVector2 v = a.Position - b.Position;
                decimal dist2 = v.X * v.X + v.Y * v.Y;
                decimal minDist = a.radius + b.radius;
                if (dist2 < minDist * minDist)
                {
                    decimal dist = Sqrt(dist2);
                    dVector2 n = v / dist;
                    decimal massRatio1 = a.radius / (a.radius + b.radius);
                    decimal massRatio2 = b.radius / (a.radius + b.radius);
                    decimal delta = 0.5m * responseCoef * (dist - minDist);
                    a.Position -= n * (massRatio2 * delta);
                    b.Position += n * (massRatio1 * delta);
                }
            }
        }

        // Handle Colliders
        foreach (var body in physicsBodies)
        {
            foreach (var collider in colliders)
            {
                dVector2 movementVector = collider.SolveIntesection(body.Position, body.radius);
                body.Position += movementVector;
            }
        }
    }

    /// <summary> Generic Newton's method for calculating the square root of a number </summary>
    public static decimal Sqrt(decimal s)
    {
        if (s < 0)
            throw new ArgumentException("Square root not defined for Decimal data type when less than zero!", "s");

        // Prevent divide-by-zero errors below. Dividing either
        // of the numbers below will yield a recurring 0 value
        // for halfS eventually converging on zero.
        if (s == 0 || s == 1E-28M) return 0;

        decimal x;
        var halfS = s / 2m;
        var lastX = -1m;
        decimal nextX;

        // Begin with an estimate for the square root.
        // Use hardware to get us there quickly.
        x = (decimal)Math.Sqrt(decimal.ToDouble(s));

        while (true)
        {
            nextX = x / 2m + halfS / x;

            // The next check effectively sees if we've ran out of
            // precision for our data type.
            if (nextX == x || nextX == lastX) break;

            lastX = x;
            x = nextX;
        }

        return nextX;
    }
}
