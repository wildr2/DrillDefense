using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class FloatProduct
{
    private float first_factor = 1;
    private Dictionary<int, float> factors;
    public float Value { get; private set; }


    // PUBLIC ACCESSORS

    public float GetFactor(UID id)
    {
        float f;
        if (factors.TryGetValue(id.Value, out f)) return f;
        return 1;
    }


    // PUBLIC MODIFIERS

    public FloatProduct(float first_factor=1)
    {
        factors = new Dictionary<int, float>();
        Value = first_factor;
    }
    public void SetFactor(float factor, UID id)
    {
        if (factor == 1) factors.Remove(id.Value);
        else factors[id.Value] = factor;
        RecalculateValue();
    }
    public void SetFactor(float first_factor)
    {
        this.first_factor = first_factor;
        RecalculateValue();
    }


    // PRIVATE / PROTECTED MODIFIERS

    protected virtual void RecalculateValue()
    {
        Value = first_factor;
        foreach (float factor in factors.Values)
        {
            Value *= factor;
        }
    }
}