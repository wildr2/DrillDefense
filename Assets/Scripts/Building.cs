using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Building : NetworkBehaviour
{
    public Player Owner { get; private set; }

    protected SpriteRenderer spriteR;
    public Transform templatePrefab;
    public abstract int Cost { get; }

    public System.Action<Building> onDestroyed;


    public virtual void Init(Player owner)
    {
        Owner = owner;
    }

    protected virtual void Awake()
    {
        spriteR = GetComponentInChildren<SpriteRenderer>();
    }
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        Drill drill = collision.collider.GetComponent<Drill>();
        if (drill != null)
        {
            if (onDestroyed != null)
                onDestroyed(this);
            Destroy(gameObject);
        }
    }
}
