using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillPlacer : Placer
{
    public float drillPlaceDist = 1.3f;
    public float atDrillMaxAngle = 45;
    public LayerMask targetUnitMask;

    private const float houseOffset = 0.5f;


    public override void Init(Player owner)
    {
        transform.up = -owner.Up;
        base.Init(owner);
    }

    protected override void UpdateTarget()
    {
        if (!Released)
        {
            Target.unit = GetNearestUnit(MousePos, 1000, targetUnitMask, true);
        }
        
        Target.valid = Target.unit != null;
        if (Target.valid)
        {
            if (Target.unit as Drill)
            {
                // Near drill
                SetAroundTargetUnit(drillPlaceDist, atDrillMaxAngle, -owner.Up);
            }
            else
            {
                // At drillhouse
                Target.up = -Target.unit.transform.up;
                Target.pos = (Vector2)Target.unit.transform.position + Target.up * houseOffset;
            }
        }
    }
}
