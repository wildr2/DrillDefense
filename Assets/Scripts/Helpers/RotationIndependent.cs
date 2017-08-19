using UnityEngine;
using System.Collections;

public class RotationIndependent : MonoBehaviour
{
    private Quaternion orig_rotation;

    private void Awake()
    {
        orig_rotation = transform.localRotation; 
    }
    private void Update()
    {
        transform.localRotation = orig_rotation * Camera.main.transform.rotation;
    }
}
