using UnityEngine;
using System.Collections;

public class AnchoredObj : MonoBehaviour
{
    private Vector3 anchor_pos, offset_pos;
    private float anchor_rotation, offset_rotation;

    public Vector3 AnchorPos
    {
        get
        {
            return anchor_pos;
        }
        set
        {
            anchor_pos = value;
            UpdatePosition();
        }
    }
    public Vector3 OffsetPos
    {
        get
        {
            return offset_pos;
        }
        set
        {
            offset_pos = value;
            UpdatePosition();
        }
    }
    public float AnchorRotation
    {
        get
        {
            return anchor_rotation;
        }
        set
        {
            anchor_rotation = value;
            UpdateRotation();
        }
    }
    public float OffsetRotation
    {
        get
        {
            return offset_rotation;
        }
        set
        {
            offset_rotation = value;
            UpdateRotation();
        }
    }

    private void Awake()
    {
        anchor_pos = transform.position;
        anchor_rotation = transform.rotation.eulerAngles.z;
    }
    private void LateUpdate()
    {
        UpdatePosition();
        UpdateRotation();

        // Reset offsets
        offset_pos = Vector3.zero;
        offset_rotation = 0;
    }
    private void UpdatePosition()
    {
        transform.position = anchor_pos + offset_pos;
    }
    private void UpdateRotation()
    {
        transform.rotation = Quaternion.Euler(0, 0, anchor_rotation + offset_rotation);
    }
}
