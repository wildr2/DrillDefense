using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Placer : MonoBehaviour
{
    public Transform graphics;
    protected Player owner;

    public Unit TargetUnit { get; protected set; }

    protected bool aimUp = true;

    private bool released = false;
    private bool targetUnitSet = false;
    private bool smoothMove = false;
    private const float smoothSpeed = 30f;

    protected Vector2 MousePos { get; private set; }
    public Vector2 Pos
    {
        get
        {
            return transform.position;
        }
    }
    public Vector2 Up
    {
        get
        {
            return transform.up;
        }
    }
    public Vector2 Aim
    {
        get
        {
            return transform.up * (aimUp ? 1 : -1);
        }
    }

    public KeyCode ReleaseKey { get; set; }
    public System.Action<Placer> onConfirm;
    public System.Action onStop;


    public virtual void Init(Player owner)
    {
        this.owner = owner;
        MousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        UpdateTransform();
    }
    public virtual void Stop()
    {
        if (onStop != null)
            onStop();
        Destroy(gameObject);
    }

    protected virtual void Update()
    {
        MousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (released)
        {
            if (targetUnitSet && TargetUnit == null)
            {
                OnTargetDestroyed();
            }
        }
        else
        {
            // Release
            if (Input.GetKeyUp(ReleaseKey))
            {
                UpdateTarget();
                released = true;
                targetUnitSet = TargetUnit != null;
                smoothMove = true;
            }
        }

        // Update position / orientation
        Vector2 oldPos = transform.position;
        Vector2 oldUp = transform.up;
        UpdateTransform();

        // Confirm
        if (Input.GetMouseButtonDown(0))
        {
            if (onConfirm != null)
                onConfirm(this);
        }

        // Cancel
        else if (Input.GetMouseButtonDown(1))
        {
            Stop();
        }

        // Smooth movement
        if (smoothMove)
        {
            Vector2 targetPos = transform.position;

            transform.position = Vector2.Lerp(oldPos, targetPos, Time.deltaTime * smoothSpeed);
            transform.up = Vector2.Lerp(oldUp, transform.up, Time.deltaTime * smoothSpeed);

            float dist = Vector2.Distance(transform.position, targetPos);
            if (dist < 0.1f)
            {
                smoothMove = false;
            }
        }
    }

    protected virtual void UpdateTarget()
    {
    }
    protected virtual void UpdateTransform()
    {
    }
    protected virtual void OnTargetDestroyed()
    {
        Stop();
    }

    protected void SetAroundTarget(float dist)
    {
        Vector2 targetPos = TargetUnit.transform.position;
        Vector2 dir = (MousePos - targetPos).normalized;

        transform.position = targetPos + dir * dist;
        transform.up = aimUp ? dir : dir * -1;
    }
    protected void SetAroundTarget(float dist, float maxAngle, Vector2 angleFrom)
    {
        Vector2 targetPos = TargetUnit.transform.position;
        Vector2 dir = MousePos - targetPos;

        float angle = Vector2.SignedAngle(angleFrom, dir);
        angle = Mathf.Clamp(angle, -maxAngle, maxAngle);

        Quaternion r = Quaternion.Euler(0, 0, angle);
        dir = (r * angleFrom).normalized;

        transform.position = targetPos + dir * dist;
        transform.up = aimUp ? dir : dir * -1;
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
