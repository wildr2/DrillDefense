using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Placer : MonoBehaviour
{
    public Transform graphics;
    protected LineRenderer aimLine;
    protected Player owner;

    protected Vector2 MousePos { get; private set; }
    public PlaceTarget Target { get; protected set; }

    public KeyCode ReleaseKey { get; set; }
    protected bool Released { get; private set; }

    private const float smoothSpeed = 30f;
    private bool smoothMove = false;

    protected bool aimUp = true;

    public Vector2 Pos
    {
        get
        {
            return Target.pos;
        }
    }
    public Vector2 Up
    {
        get
        {
            return Target.up;
        }
    }
    public Vector2 Aim
    {
        get
        {
            return Target.up * (aimUp ? 1 : -1);
        }
    }

    public System.Action<Placer> onConfirm;
    public System.Action onStop;


    public virtual void Init(Player owner)
    {
        this.owner = owner;

        aimLine = GetComponent<LineRenderer>();
        if (aimLine != null)
        {
            //aimLine.enabled = false;
        }

        Target = new PlaceTarget();
        Update();
    }
    public virtual void Stop()
    {
        if (onStop != null)
            onStop();

        if (Target.unit)
        {
            Target.unit.ShowSelectionHighlight(false);
        }
        Destroy(gameObject);
    }


    protected virtual void Update()
    {
        MousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        bool hadTarget = Target.valid;
        Unit oldTargetUnit = Target.unit;

        // Pre Release
        if (!Released) 
        {
            UpdatePreRelease();
        }
        // Post Release
        else
        {
            UpdatePostRelease();
        }

        // Target on / off check
        if ((Target.valid && !hadTarget))
        {
            OnTargetOn();
        }
        else if (!Target.valid && hadTarget)
        {
            OnTargetOff();
        }
        if (oldTargetUnit != Target.unit)
        {
            OnTargetUnitChange(oldTargetUnit);
        }
    }
    private void UpdatePreRelease()
    {
        // Attach to mouse
        transform.position = MousePos;

        // Target
        UpdateTarget();

        // Immediate confirm if no need to aim
        if (Target.valid && Input.GetMouseButtonDown(0))
        {
            Confirm();
        }
        // Cancel
        else if (Input.GetMouseButtonDown(1))
        {
            Stop();
        }

        // Release
        if (Input.GetKeyUp(ReleaseKey))
        {
            Release();
            return;
        }
    }
    private void UpdatePostRelease()
    {
        UpdateTarget();

        // Confirm
        if (Input.GetMouseButtonDown(0))
        {
            Confirm();
        }
        // Cancel
        else if (Input.GetMouseButtonDown(1))
        {
            Stop();
        }

        // Position / orientation
        UpdatePostReleaseTransform();

        // Aimline
        if (aimLine != null)
        {
            SetAimLine(transform.position, transform.position + transform.up * 1000 * (aimUp ? 1 : -1));
        }
    }
    private void UpdatePostReleaseTransform()
    {
        if (smoothMove)
        {
            transform.position = Vector2.Lerp(transform.position, Target.pos, Time.deltaTime * smoothSpeed);
            transform.up = Vector2.Lerp(transform.up, Target.up, Time.deltaTime * smoothSpeed);

            float dist = Vector2.Distance(transform.position, Target.pos);
            if (dist < 0.1f)
            {
                smoothMove = false;
            }
        }
        else
        {
            transform.position = Target.pos;
            transform.up = Target.up;
        }
    }

    protected virtual void UpdateTarget()
    {
    }
    protected virtual void Release()
    {
        if (Target.valid)
        {
            Released = true;
            smoothMove = true;
            UpdatePostRelease();
            if (Target.unit)
            {
                Target.unit.ShowSelectionHighlight(false);
            }
        }
        else
        {
            Stop();
        }
    }
    protected virtual void OnTargetUnitChange(Unit oldUnit)
    {
        if (oldUnit != null)
        {
            oldUnit.ShowSelectionHighlight(false);
        }
        if (Target.unit)
        {
            Target.unit.ShowSelectionHighlight(true);
        }
    }
    protected virtual void OnTargetOn()
    {
        if (aimLine != null)
        {
            aimLine.enabled = true;
        }
    }
    protected virtual void OnTargetOff()
    {
        if (Released)
        {
            Stop();
        }
        else
        {
            if (aimLine != null)
            {
                aimLine.enabled = false;
            }
        }
    }
    protected virtual void Confirm()
    {
        if (onConfirm != null)
            onConfirm(this);
    }

    protected void SetAroundTargetUnit(float dist)
    {
        Vector2 unitPos = Target.unit.transform.position;
        Vector2 aim = (MousePos - unitPos).normalized;

        Target.pos = unitPos + aim * dist;
        Target.up = aimUp ? aim : aim * -1;
    }
    protected void SetAroundTargetUnit(float dist, float maxAngle, Vector2 angleFrom)
    {
        Vector2 unitPos = Target.unit.transform.position;
        Vector2 aim = MousePos - unitPos;

        float angle = Vector2.SignedAngle(angleFrom, aim);
        angle = Mathf.Clamp(angle, -maxAngle, maxAngle);

        Quaternion r = Quaternion.Euler(0, 0, angle);
        aim = (r * angleFrom).normalized;

        Target.pos = unitPos + aim * dist;
        Target.up = aimUp ? aim : aim * -1;
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

    private void SetAimLine(Vector2 p1, Vector2 p2)
    {
        aimLine.SetPosition(0, new Vector3(p1.x, p1.y, -5));
        aimLine.SetPosition(1, new Vector3(p2.x, p2.y, -5));
    }


    public class PlaceTarget
    {
        public Vector2 pos = new Vector2();
        public Vector2 up = new Vector2();
        public Unit unit = null;
        public bool valid = false;
    }
}

