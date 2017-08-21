using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamController : MonoBehaviour
{
    private Camera cam;
    private Vector3 targetPos;
    private float scrollSpeed = 10;
    private float scrollSharpness = 40;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetPos = transform.position;
    }
    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        targetPos += Vector3.up * scroll * scrollSpeed;
        //transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * scrollSharpness);
        transform.position = targetPos;
    }
}
