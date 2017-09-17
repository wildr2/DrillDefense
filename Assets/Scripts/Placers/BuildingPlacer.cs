using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPlacer : Placer
{
    public float targetPlaceDist = 1.25f;
    public float atDrillMaxAngle = 45;
    public LayerMask targetUnitMask;
    private Ground ground;


    public void Init(Player owner, Ground ground, Building buildingPrefab)
    {
        this.ground = ground;
        aimUp = false;

        SetGoldCost(buildingPrefab.Cost);

        base.Init(owner);
    }
    protected override void UpdateTarget()
    {
        if (!Released)
        {
            Unit unit = GetNearestUnit(MousePos, 1000, targetUnitMask, true);
            if (unit)
            {
                float unitDist = Vector2.Distance(unit.transform.position, MousePos);
                float groundDist = Mathf.Abs(ground.GetHeightAt(MousePos.x, owner.IsTop) - MousePos.y);

                Target.unit = unitDist < groundDist ? unit : null;
            }
            else
            {
                Target.unit = null;
            }
        }

        if (Target.unit)
        {
            SetAroundTargetUnit(targetPlaceDist, atDrillMaxAngle, -owner.Up);
        }
        else
        {
            ground.SetOnSurface(out Target.pos, out Target.up, MousePos.x, owner.IsTop);
        }

        Target.valid = true;
    }
}
