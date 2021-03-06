﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Unit : NetworkBehaviour
{
    public Player Owner { get; private set; }
    public float VisionRadius { get; private set; }
    public abstract float KillGold { get; }

    public Transform graphics;
    public Transform selectionHighlight;
    public Transform visionArea;

    public System.Action<Unit> onDestroyed;

    
    public virtual void Init(Player owner)
    {
        Owner = owner;
        visionArea.gameObject.SetActive(false);
        VisionRadius = visionArea.transform.localScale.x / 2f;

        // Setup vision
        FindObjectOfType<Ground>().RegisterUnitWithVisionSys(this);
    }
    public virtual void SetVisible(bool visible = true)
    {
        if (graphics.gameObject.activeInHierarchy != visible)
        {
            graphics.gameObject.SetActive(visible);
        }       
    }
    public virtual void ShowSelectionHighlight(bool show = true)
    {
        if (selectionHighlight.gameObject.activeInHierarchy != show)
        {
            selectionHighlight.gameObject.SetActive(show);
        }
    }

    protected void Kill(Player killer)
    {
        if (killer != Owner)
            killer.gold += KillGold;
        Destroy(gameObject);
    }

    protected virtual void Awake()
    {
        
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        SetVisible(false);
    }
    protected virtual void OnDestroy()
    {
        if (onDestroyed != null)
            onDestroyed(this);
    }
}
