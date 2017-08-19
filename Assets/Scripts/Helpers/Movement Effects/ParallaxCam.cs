using UnityEngine;
using System.Collections;

public class ParallaxCam : MonoBehaviour
{
    public float dist = 0.5f;

    private void LateUpdate()
    {
        Vector2 pos = Camera.main.transform.position * (1 - dist);
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        transform.rotation = Camera.main.transform.rotation;
    }
}
