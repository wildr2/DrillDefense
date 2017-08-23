using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamController : MonoBehaviour
{
    private Vector3 targetPos;
    private float scrollSpeed = 10;
    //private float scrollSharpness = 20;

    private void Awake()
    {
        targetPos = transform.position;
    }
    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        targetPos += Camera.main.transform.up * scroll * scrollSpeed;
        //transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * scrollSharpness);
        transform.position = targetPos;
    }
}
