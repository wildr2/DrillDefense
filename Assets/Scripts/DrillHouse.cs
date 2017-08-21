using System;
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
        return gold >= drillCost;
    }

    public Drill LaunchDrill()
    {
        Drill drill = Instantiate(drillPrefab);
        drill.transform.position = transform.position - transform.up * 0.5f;
        drill.SetDirection(-transform.up);

        Physics2D.IgnoreCollision(GetComponent<Collider2D>(),
            drill.GetComponent<Collider2D>());

        return drill;
    }

    protected override void Awake()
    {
        base.Awake();
    }
    private void Update()
    {
    }
}
