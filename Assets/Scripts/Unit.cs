using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Unit : NetworkBehaviour
{
    public Player Owner { get; private set; }
    public abstract float VisionRadius { get; }
    public Transform graphics;

    public System.Action<Unit> onDestroyed;


    public virtual void Init(Player owner)
    {
        Owner = owner;
        FindObjectOfType<Ground>().RegisterVisionUnit(this);
    }

    protected virtual void SetVisible(bool visible = true)
    {
        graphics.gameObject.SetActive(visible);
    }
    protected virtual void OnDestroy()
    {
        if (onDestroyed != null)
            onDestroyed(this);
    }
}
