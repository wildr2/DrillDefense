using UnityEngine;
using System.Collections;

public class ZoomIndependent : MonoBehaviour
{
    public float factor = 0.75f; // between 0 and 1

    private Vector3 orig_scale;
    private float orig_ortho;

    private void Awake()
    {
        orig_scale = transform.localScale;
        orig_ortho = Camera.main.orthographicSize;

        factor = Mathf.Clamp01(factor);
    }
    private void Update()
    {
        transform.localScale = (1f - factor) * orig_scale + factor * orig_scale * (Camera.main.orthographicSize / orig_ortho);
    }
}
