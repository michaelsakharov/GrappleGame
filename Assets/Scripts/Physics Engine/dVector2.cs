using System;

[Serializable]
public struct dVector2
{
    public UnityDecimal X;
    public UnityDecimal Y;

    public dVector2(decimal x, decimal y)
    {
        X = new(x);
        Y = new(y);
    }


    public static dVector2 operator +(dVector2 left, dVector2 right) => new dVector2(left.X.value + right.X.value, left.Y.value + right.Y.value);
    public static dVector2 operator +(dVector2 vector, decimal adder) => new dVector2(vector.X.value + adder, vector.Y.value + adder);
    public static dVector2 operator -(dVector2 left, dVector2 right) => new dVector2(left.X.value - right.X.value, left.Y.value - right.Y.value);
    public static dVector2 operator -(dVector2 vector, decimal subtractor) => new dVector2(vector.X.value - subtractor, vector.Y.value - subtractor);
    public static dVector2 operator *(dVector2 vector, dVector2 multiplier) => new dVector2(vector.X.value * multiplier.X.value, vector.Y.value * multiplier.Y.value);
    public static dVector2 operator *(dVector2 vector, decimal scalar) => new dVector2(vector.X.value * scalar, vector.Y.value * scalar);

    public static dVector2 operator /(dVector2 vector, decimal divisor)
    {
        if (divisor == 0) throw new DivideByZeroException("Division by zero.");
        return new dVector2(vector.X.value / divisor, vector.Y.value / divisor);
    }

    public static bool operator ==(dVector2 left, dVector2 right) => left.X.value == right.X.value && left.Y.value == right.Y.value;

    public static bool operator !=(dVector2 left, dVector2 right) => !(left == right);

    public override bool Equals(object obj)
    {
        if (obj is not dVector2) return false;
        dVector2 other = (dVector2)obj;
        return X.value == other.X.value && Y.value == other.Y.value;
    }

    public override int GetHashCode() => X.value.GetHashCode() ^ Y.value.GetHashCode();

    public static decimal Length(dVector2 v) => PhysicsManager.Sqrt((v.X.value * v.X.value) + (v.Y.value * v.Y.value));
    public static dVector2 Normalize(dVector2 v) => v * (1.0m / PhysicsManager.Sqrt((v.X.value * v.X.value) + (v.Y.value * v.Y.value)));
    public static dVector2 Min(dVector2 a, dVector2 b) => new(a.X.value < b.X.value ? a.X.value : b.X.value, a.Y.value < b.Y.value ? a.Y.value : b.Y.value);
    public static dVector2 Max(dVector2 a, dVector2 b) => new(a.X.value > b.X.value ? a.X.value : b.X.value, a.Y.value > b.Y.value ? a.Y.value : b.Y.value);
    public static dVector2 Abs(dVector2 v) => new(Math.Abs(v.X.value), Math.Abs(v.Y.value));
}
