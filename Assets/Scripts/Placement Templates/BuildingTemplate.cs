using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingTemplate : PlacementTemplate
{
    public float targetPlaceDist = 1.25f;
    public float targetAttachDist = 1;
    public LayerMask targetUnitMask;
    private Ground ground;

    public void Init(Player owner, Ground ground)
    {
        this.ground = ground;
        base.Init(owner);
    }
    protected override void UpdateTarget()
    {
        if (!TargetUnit)
        {
            TargetUnit = GetNearestUnit(MousePos, targetAttachDist, targetUnitMask, true);
        }
    }
    protected override void UpdateTransform()
    {
        if (TargetUnit)
        {
            SetAroundTarget(targetPlaceDist);
        }
        else
        { 
            ground.SetOnSurface(transform, MousePos.x, owner.IsTop);
        }
    }
}
