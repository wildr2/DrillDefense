using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillTemplate : PlacementTemplate
{
    public float drillPlaceDist = 1.25f;
    public float targetAttachDist = 1;
    public LayerMask targetUnitMask;

    private const float houseOffset = 0.35f;

    
    protected override void UpdateTarget()
    {
        if (!TargetUnit || TargetUnit as DrillHouse)
        {
            TargetUnit = GetNearestUnit(MousePos, targetAttachDist, targetUnitMask, true);
        }
    }
    protected override void UpdateTransform()
    {
        if (TargetUnit)
        {
            if (TargetUnit as Drill)
            {
                // Near drill
                SetAroundTarget(drillPlaceDist, true, 45, TargetUnit.transform.up);
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
