using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AnchoredObj))]
public class FloatingObj : MonoBehaviour
{
    private AnchoredObj anchor;

    public float speed = 1;
    public float intensity = 1;
    public float rotation_degrees = 1;


    private void Awake()
    {
        anchor = GetComponent<AnchoredObj>();
    }
    private void Start()
    {
        StartCoroutine(Float());
    }
    private IEnumerator Float()
    {
        float t = Random.value * 360;
        float timer = 0;
        float rand_speed = 0;

        while (true)
        {
            timer -= Time.deltaTime;
            if (timer < 0)
            {
                rand_speed = Random.Range(0.4f, 0.6f) * speed;
                timer = Random.Range(0.5f, 1.5f);
            }

            t += Time.deltaTime * rand_speed;
            anchor.OffsetPos = anchor.OffsetPos + (Vector3)(Vector2.up * Mathf.Sin(t) * 0.3f * intensity);
            anchor.OffsetRotation = anchor.OffsetRotation + Mathf.Sin(t) * rotation_degrees;

            yield return null;
        }
    }
}
