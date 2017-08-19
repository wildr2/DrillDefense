using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AnchoredObj))]
public class ShakingObj : MonoBehaviour
{
    private AnchoredObj anchor;
    private float current_intensity = 0; // the intensity of the biggest shake currently happening

    // Positions in the perlin noise field for x and y movement
    private Vector2 perlin_x;
    private Vector2 perlin_y;


    public Coroutine Shake(float duration, float intensity = 1, float speed = 1)
    {
        return StartCoroutine(ShakeRoutine(duration, intensity, speed));
    }
    public Coroutine Shake(ShakeParams shake_params)
    {
        return Shake(shake_params.duration, shake_params.intensity, shake_params.speed);
    }
    public void StopShake(Coroutine shake_routine)
    {
        if (shake_routine != null) StopCoroutine(shake_routine);
    }

    protected virtual void Awake()
    {
        anchor = GetComponent<AnchoredObj>();

        // Start at random positions in the perlin field
        perlin_x = Tools.RandomDirection2D() * Random.value * 100f;
        perlin_y = Tools.RandomDirection2D() * Random.value * 100f;
    }
    private IEnumerator ShakeRoutine(float duration, float intensity, float speed)
    {
        float t = 1;
        while (t > 0)
        {
            // Elapsed time
            t = Mathf.Max(t - Time.deltaTime / duration, 0);

            // only update shaking if there is not a bigger shake happening (another routine)
            while (current_intensity > intensity) yield return null;

            // Update current intensity
            float damper = Mathf.Clamp01(t * 3f);
            current_intensity = intensity * damper;

            // Shake
            perlin_x.x += Time.deltaTime * speed * 10f;
            perlin_y.x += Time.deltaTime * speed * 10f;

            Vector2 offset = new Vector2(
                Mathf.PerlinNoise(perlin_x.x, perlin_x.y) - 0.5f,
                Mathf.PerlinNoise(perlin_y.x, perlin_y.y) - 0.5f);

            anchor.OffsetPos = anchor.OffsetPos + (Vector3)offset * current_intensity * 0.5f;

            yield return null;
        }
    }
}
public class ShakeParams
{
    public float duration = 1;
    public float intensity = 1;
    public float speed = 1;

    public ShakeParams(float duration, float intensity, float speed)
    {
        this.duration = duration;
        this.intensity = intensity;
        this.speed = speed;
    }
}
