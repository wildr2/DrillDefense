using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Unit : NetworkBehaviour
{
    public Player Owner { get; private set; }
    public abstract float VisionRadius { get; }
    public abstract float KillGold { get; }
    public Transform graphics;

    public System.Action<Unit> onDestroyed;

    
    public virtual void Init(Player owner)
    {
        Owner = owner;

        // Setup vision
        FindObjectOfType<Ground>().RegisterUnitWithVisionSys(this);
    }
    public virtual void SetVisible(bool visible = true)
    {
        if (graphics.gameObject.activeInHierarchy != visible)
            graphics.gameObject.SetActive(visible);
    }

    protected void Kill(Player killer)
    {
        if (killer != Owner)
            killer.gold += KillGold;
        Destroy(gameObject);
    }

    protected virtual void Awake()
    {
        SetVisible(false);
    }
    protected virtual void OnDestroy()
    {
        if (onDestroyed != null)
            onDestroyed(this);
    }
}
