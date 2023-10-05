using UnityEngine;

[System.Serializable]
public struct UnityDecimal : ISerializationCallbackReceiver
{
    public decimal value;
    [SerializeField]
    private int[] data;

    public UnityDecimal(decimal v)
    {
        this.value = v;
        data = decimal.GetBits(v);
    }

    public void OnBeforeSerialize() => data = decimal.GetBits(value);

    public void OnAfterDeserialize()
    {
        if (data != null && data.Length == 4)
            value = new(data);
    }

    public static implicit operator decimal(UnityDecimal unityDecimal)
    {
        return unityDecimal.value;
    }

    public static implicit operator UnityDecimal(decimal decimalValue)
    {
        return new UnityDecimal(decimalValue);
    }

    public static UnityDecimal operator +(UnityDecimal left, decimal right)
    {
        return new UnityDecimal(left.value + right);
    }

    public override string ToString()
    {
        return value.ToString();
    }
}
