using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Building : MonoBehaviour
{
    protected SpriteRenderer sRenderer;
    public abstract int Cost { get; }

    public Transform templatePrefab;


    protected void Awake()
    {
        sRenderer = GetComponent<SpriteRenderer>();
    }
}
