using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombPlacer : Placer
{
    public LayerMask targetUnitMask;


    protected override void UpdateTarget()
    {
        if (!Released)
        {
            Target.unit = GetNearestUnit(MousePos, 1000, targetUnitMask, true);
        }

        Target.valid = Target.unit != null;
        if (Target.valid)
        {
            Target.pos = Target.unit.transform.position;
        }
    }
}
