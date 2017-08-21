using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Building : MonoBehaviour
{
    protected SpriteRenderer spriteR;
    public Transform templatePrefab;
    public abstract int Cost { get; }


    protected virtual void Awake()
    {
        spriteR = GetComponentInChildren<SpriteRenderer>();
    }
}
