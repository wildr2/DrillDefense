using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillPlacer : Placer
{
    public float drillPlaceDist = 1.3f;
    public float targetAttachDist = 1;
    public float atDrillMaxAngle = 45;
    public LayerMask targetUnitMask;

    private const float houseOffset = 0.5f;

    
    protected override void UpdateTarget()
    {
        TargetUnit = GetNearestUnit(MousePos, targetAttachDist, targetUnitMask, true);
    }
    protected override void UpdateTransform()
    {
        if (TargetUnit)
        {
            if (TargetUnit as Drill)
            {
                // Near drill
                SetAroundTarget(drillPlaceDist, atDrillMaxAngle, -owner.Up);
            }
            else
            {
                // At drillhouse
                transform.up = -TargetUnit.transform.up;
                transform.position = TargetUnit.transform.position + transform.up * houseOffset;
            }
        }
        else
        {
            transform.position = MousePos;
            transform.up = -owner.Up;
        }
    }
}
