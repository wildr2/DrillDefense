using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DrillHouse : Building
{
    public override int Cost { get { return 50; } }
    public override float KillGold { get { return 50; } }
    
    

    public Drill drillPrefab;
    public SpriteRenderer roofSpriteR;

    public override void Init(Player owner)
    {
        base.Init(owner);
        roofSpriteR.color = owner.Color;
    }
}
