using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPlacer : Placer
{
    public float targetPlaceDist = 1.25f;
    public float targetAttachDist = 1;
    public float atDrillMaxAngle = 45;
    public LayerMask targetUnitMask;
    private Ground ground;

    private bool targetGround = false;


    public void Init(Player owner, Ground ground)
    {
        this.ground = ground;
        aimUp = false;
        base.Init(owner);
    }
    protected override void UpdateTarget()
    {
        Unit unit = GetNearestUnit(MousePos, targetAttachDist, targetUnitMask, true);
        if (unit)
        {
            float unitDist = Vector2.Distance(unit.transform.position, MousePos);
            float groundDist = Mathf.Abs(ground.GetHeightAt(MousePos.x, owner.IsTop) - MousePos.y);

            TargetUnit = unitDist < groundDist ? unit : null;
        }
        else
        {
            TargetUnit = null;
        }
        targetGround = TargetUnit == null;
    }
    protected override void UpdateTransform()
    {
        if (TargetUnit)
        {
            SetAroundTarget(targetPlaceDist, atDrillMaxAngle, -owner.Up);
        }
        else if (targetGround)
        { 
            ground.SetOnSurface(transform, MousePos.x, owner.IsTop);
        }
        else
        {
            transform.position = MousePos;
        }
    }
}
