using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlacementTemplate : MonoBehaviour
{
    public Transform graphics;
    protected Player owner;

    public Unit TargetUnit { get; protected set; }
    private bool hadTargetLast = false;

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

        if (TargetUnit == null && hadTargetLast)
        {
            OnTargetDestroyed();
            return;
        }

        UpdateTarget();
        UpdateTransform();

        hadTargetLast = TargetUnit != null;
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

    protected void SetAroundTarget(float dist, bool pointUp = false)
    {
        Vector2 targetPos = TargetUnit.transform.position;
        Vector2 dir = (targetPos - MousePos).normalized;
        if (pointUp) dir *= -1;

        transform.position = targetPos + dir * dist;
        transform.up = dir;
    }
    protected void SetAroundTarget(float dist, bool pointUp, float maxAngle, Vector2 angleFrom)
    {
        Vector2 targetPos = TargetUnit.transform.position;
        Vector2 dir = targetPos - MousePos;
        if (pointUp) dir *= -1;

        float angle = Vector2.SignedAngle(angleFrom, dir);
        angle = Mathf.Clamp(angle, -maxAngle, maxAngle);
        Tools.Log(angle);

        Quaternion r = Quaternion.Euler(0, 0, angle);
        dir = (r * angleFrom).normalized;

        transform.up = dir;
        transform.position = targetPos + dir * dist;
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

    private static float UnwindAngle(float a = 3.14f)
    {
        a = Tools.Mod(a, Mathf.PI * 2);
        if (a < 0) a += 360;
        return a;
    }
}
