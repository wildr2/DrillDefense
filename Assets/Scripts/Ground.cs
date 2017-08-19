using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    public Color groundColor = Color.white;

    private SpriteRenderer sRenderer;
    private Sprite sprite;
    private Texture2D tex;
    private float width, height; // world units
    private float resolution = 10; // pixels per world unit

    private void Awake()
    {
        sRenderer = GetComponent<SpriteRenderer>();

        width = transform.localScale.x;
        height = transform.localScale.y;
        transform.localScale = new Vector3(1, 1, 1);

        tex = new Texture2D((int)(resolution*width), (int)(resolution*height), TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        FillTexture(tex, groundColor);

        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), resolution);
        sRenderer.sprite = sprite;
        sRenderer.color = Color.white;

        //DrawLine(tex, new Vector2(0, 0), new Vector2(tex.width, tex.height), Color.clear);
        //DrawLine(tex, new Vector2(0, 1), new Vector2(tex.width-1, tex.height), Color.clear);
        DrawLine(tex, new Vector2(0, tex.height / 2f), new Vector2(tex.width, tex.height / 2f), Color.clear);
        DrawLine(tex, new Vector2(tex.width / 2f, 0), new Vector2(tex.width / 2f, tex.height), Color.clear);

        tex.Apply();
    }

    /// <summary>
    /// http://answers.unity3d.com/questions/244417/create-line-on-a-texture.html
    /// </summary>
    /// <param name="tex"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="col"></param>
    private static void DrawLine(Texture2D tex, Vector2 p1, Vector2 p2, Color col)
    {
        Vector2 t = p1;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
        {
            t = Vector2.Lerp(p1, p2, ctr);
            ctr += frac;
            tex.SetPixel((int)t.x, (int)t.y, col);
        }
    }
    private static void FillTexture(Texture2D tex, Color color)
    {
        Color32[] colors = tex.GetPixels32();

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = color;
        }

        tex.SetPixels32(colors);
    }
}
