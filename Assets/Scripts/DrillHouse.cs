﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DrillHouse : Building
{
    public Drill drillPrefab;
    public override int Cost { get { return 50; } }
    public const int drillCost = 10;

    public SpriteRenderer roofSpriteR;


    public bool CanLaunchDrill(Player player)
    {
        return player == Owner && player.GetGold() >= drillCost;
    }

    public Drill LaunchDrill()
    {
        Drill drill = Instantiate(drillPrefab);
        drill.transform.position = transform.position - transform.up * 0.5f;
        drill.SetDirection(-transform.up);
        NetworkServer.Spawn(drill.gameObject);

        if (!isClient) OnLaunchDrill(drill);
        RpcOnLaunchDrill(drill.netId);

        return drill;
    }

    protected override void Awake()
    {
        base.Awake();
    }
    public override void Init(Player owner)
    {
        base.Init(owner);
        roofSpriteR.color = owner.Color;
    }

    private void OnLaunchDrill(Drill drill)
    {
        drill.Init(Owner);

        Physics2D.IgnoreCollision(GetComponent<Collider2D>(),
            drill.GetComponent<Collider2D>());
    }
    [ClientRpc]
    private void RpcOnLaunchDrill(NetworkInstanceId drillNetId)
    {
        OnLaunchDrill(ClientScene.FindLocalObject(drillNetId).GetComponent<Drill>());
    }
}
