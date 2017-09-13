using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlacementTemplate : MonoBehaviour
{
    public Transform graphics;
    protected Player owner;

    private Unit targetUnit;
    public Unit TargetUnit
    {
        get { return targetUnit; }
        protected set
        {
            targetUnit = value;
            targetSet = value != null;
        }
    }
    private bool targetSet = false;

    protected Vector2 MousePos { get; private set; }


    public virtual void Init(Player owner)
    {
        this.owner = owner;

        gameObject.SetActive(true);
        UpdatePlacement();
    }

    protected virtual void Awake()
    {
        gameObject.SetActive(false);
    }
    protected virtual void Update()
    {
        UpdatePlacement();
    }
    private void UpdatePlacement()
    {
        MousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (TargetUnit == null && targetSet)
        {
            OnTargetDestroyed();
            return;
        }

        UpdateTarget();
        UpdateTransform();
    }

    protected virtual void UpdateTarget()
    {
    }
    protected virtual void UpdateTransform()
    {
    }
    protected virtual void OnTargetDestroyed()
    {
        Destroy(gameObject);
    }

    protected void SetAroundTarget(float dist, bool pointUp)
    {
        Vector2 targetPos = TargetUnit.transform.position;
        Vector2 dir = (MousePos - targetPos).normalized;

        transform.position = targetPos + dir * dist;
        transform.up = pointUp ? dir : dir * -1;
    }
    protected void SetAroundTarget(float dist, bool pointUp, float maxAngle, Vector2 angleFrom)
    {
        Vector2 targetPos = TargetUnit.transform.position;
        Vector2 dir = MousePos - targetPos;

        float angle = Vector2.SignedAngle(angleFrom, dir);
        angle = Mathf.Clamp(angle, -maxAngle, maxAngle);

        Quaternion r = Quaternion.Euler(0, 0, angle);
        dir = (r * angleFrom).normalized;

        transform.position = targetPos + dir * dist;
        transform.up = pointUp ? dir : dir * -1;
    }
    protected Unit GetNearestUnit(Vector2 point, float range, LayerMask unitMask, bool friendlyOnly = false)
    {
        Unit nearest = null;
        float minDist = range + 1;

        Collider2D[] cols = Physics2D.OverlapCircleAll(point, range, unitMask);
        foreach (Collider2D col in cols)
        {
            Unit drill = col.GetComponent<Unit>();
            if (drill == null) continue;
            if (friendlyOnly && drill.Owner != owner) continue;

            float dist = Vector2.Distance(point, drill.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = drill;
            }
        }
        return nearest;
    }
}
