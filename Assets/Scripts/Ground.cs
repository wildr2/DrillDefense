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



    public void DrillLine(Vector2 p1, Vector2 p2, float width)
    {
        int weight = Mathf.CeilToInt(resolution * width);

        DrawLineWeighted(tex, WorldToTexPos(p1), WorldToTexPos(p2),
            weight, Color.clear);
        tex.Apply();
    }


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
        //DrawLine(tex, new Vector2(0, 1), new Vector2(tex.width - 1, tex.height), Color.clear);
        //DrawLine(tex, new Vector2(0, tex.height / 2f), new Vector2(tex.width, tex.height / 2f), Color.clear);
        //DrawLine(tex, new Vector2(tex.width / 2f, 0), new Vector2(tex.width / 2f, tex.height), Color.clear);
        //DrawLineWeighted(tex, new Vector2(10, 0), new Vector2(80, tex.height), 10, Color.clear);

        //DrillLine(new Vector2(0, 5), new Vector2(0, -5), 1);

        tex.Apply();
    }

    private Vector2 WorldToTexPos(Vector2 worldPos)
    {
        Vector2 ret = worldPos;
        ret -= (Vector2)transform.position;
        ret.x = ((ret.x / width) + 0.5f) * tex.width;
        ret.y = ((ret.y / height) + 0.5f) * tex.height;
        return ret;
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
            int x = (int)t.x;
            int y = (int)t.y;
            if (x > -1 && x < tex.width && y > -1 && y < tex.height)
            {
                tex.SetPixel(x, y, col);
            }
        }
    }
    private static void DrawLineWeighted(Texture2D tex, Vector2 p1, Vector2 p2, int w, Color col)
    {
        int leftW = Mathf.FloorToInt(w / 2f);
        int rightW = Mathf.CeilToInt(w / 2f);

        Vector2 normal = Vector3.Cross(p2 - p1, Vector3.forward).normalized;
        for (int x = -leftW; x < rightW; ++x)
        {
            DrawLine(tex, p1 + normal * x, p2 + normal * x, col);
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
