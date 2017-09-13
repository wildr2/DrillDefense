using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingTemplate : PlacementTemplate
{
    public float targetPlaceDist = 1.25f;
    public float targetAttachDist = 1;
    public float atDrillMaxAngle = 45;
    public LayerMask targetUnitMask;
    private Ground ground;

    public void Init(Player owner, Ground ground)
    {
        this.ground = ground;
        base.Init(owner);
    }
    protected override void UpdateTarget()
    {
        Unit potentialTarget = GetNearestUnit(MousePos, targetAttachDist, targetUnitMask, true);
        if (potentialTarget)
        {
            float potentialTargetDist = Vector2.Distance(potentialTarget.transform.position, MousePos);
            float groundDist = Mathf.Abs(ground.GetHeightAt(MousePos.x, owner.IsTop) - MousePos.y);

            TargetUnit = potentialTargetDist < groundDist ? potentialTarget : null;
        }
        else
        {
            TargetUnit = null;
        } 
    }
    protected override void UpdateTransform()
    {
        if (TargetUnit)
        {
            SetAroundTarget(targetPlaceDist, false, atDrillMaxAngle, -owner.Up);
        }
        else
        { 
            ground.SetOnSurface(transform, MousePos.x, owner.IsTop);
        }
    }
}
