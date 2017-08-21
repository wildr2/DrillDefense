﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillHouse : Building
{
    public Drill drillPrefab;
    public override int Cost { get { return 50; } }
    public const int drillCost = 10;


    public bool CanLaunchDrill(int gold)
    {
        return gold > drillCost;
    }

    public void LaunchDrill()
    {
        Drill drill = Instantiate(drillPrefab);
        drill.transform.position = transform.position - transform.up * 0.5f;
        drill.SetDirection(-transform.up);
    }

    protected override void Awake()
    {
        base.Awake();
    }
    private void Update()
    {
    }
}
