using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillHouse : Building
{
    public Drill drillPrefab;
    public override int Cost { get { return 50; } }

    new private void Awake()
    {
        base.Awake();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LaunchDrill();
        }
    }

    private void LaunchDrill()
    {
        Drill drill = Instantiate(drillPrefab);
        drill.transform.position = (Vector2)transform.position - Vector2.up * 0.5f;
        drill.SetDirection(-1);
    }
}
