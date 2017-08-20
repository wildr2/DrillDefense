using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GroundState { UnDug, Dug, None }

public class Ground : MonoBehaviour
{
    public Color unDugColor = Color.white;
    private Color dugColor = Color.clear;

    private SpriteRenderer sRenderer;
    private Sprite sprite;
    private Texture2D tex;

    // number of pixels high (from bottom of image) for each pixel along horizontal
    private int[] topHeightMap, botHeightMap; 

    private float width, height; // world units
    private float resolution = 10; // pixels per world unit



    public GroundState GetStateAt(Vector2 worldPos)
    {
        return GetStateAtTexPos(WorldToTexPos(worldPos));
    }
    /// <summary>
    /// Return whether any non-transparent pixels in a sprite overlap ground in specified state
    /// </summary>
    /// <param name="tex"></param>
    /// <returns></returns>
    public bool SpriteOverlaps(SpriteRenderer sr, GroundState state=GroundState.UnDug)
    {
        // TODO: interpolate texPos instead of worldPos to avoid calls to WorldToTexPos

        // Check if sprite bounds overlap ground bounds at all
        if (!BoundsOverlap(sr.bounds))
        {
            return false;
        }

        // Check pixel by pixel
        Vector2 pos = sr.transform.TransformPoint(sr.sprite.bounds.min); // world pos of current sprite pixel
        Vector2 dx = sr.transform.TransformVector(new Vector2(1f / sr.sprite.pixelsPerUnit, 0));
        Vector2 dy = sr.transform.TransformVector(new Vector2(0, 1f / sr.sprite.pixelsPerUnit));

        for (int x = 0; x < sr.sprite.texture.width; ++x)
        {
            for (int y = 0; y < sr.sprite.texture.height; ++y)
            {
                if (sr.sprite.texture.GetPixel(x, y).a > 0)
                {
                    if (GetStateAt(pos) == state)
                    {
                        return true;
                    }
                }
                pos += dy;
            }
            pos -= dy * sr.sprite.texture.width;
            pos += dx;
        }

        return false;
    }

    /// <summary>
    /// Returns whether any ground was dug successfully
    /// </summary>
    /// <param name="sr"></param>
    /// <returns></returns>
    public bool DigWithSprite(SpriteRenderer sr)
    {
        // TODO: interpolate texPos instead of worldPos to avoid calls to WorldToTexPos

        // Check if sprite bounds overlap ground bounds at all
        if (!BoundsOverlap(sr.bounds))
        {
            return false;
        }

        //  Pixel by pixel
        Vector2 pos = sr.transform.TransformPoint(sr.sprite.bounds.min); // world pos of current sprite pixel
        Vector2 dx = sr.transform.TransformVector(new Vector2(1f / sr.sprite.pixelsPerUnit, 0));
        Vector2 dy = sr.transform.TransformVector(new Vector2(0, 1f / sr.sprite.pixelsPerUnit));

        bool dugAny = false;

        for (int x = 0; x < sr.sprite.texture.width; ++x)
        {
            for (int y = 0; y < sr.sprite.texture.height; ++y)
            {
                if (sr.sprite.texture.GetPixel(x, y).a > 0)
                {
                    Vector2 texPos = WorldToTexPos(pos);
                    if (GetStateAtTexPos(texPos) == GroundState.UnDug)
                    {
                        // Dig
                        DigAt(texPos);
                        dugAny = true;
                    }
                }
                pos += dy;
            }
            pos -= dy * sr.sprite.texture.width;
            pos += dx;
        }

        tex.Apply();
        return dugAny;
    }
    public void DrillLine(Vector2 p1, Vector2 p2, float width)
    {
        int weight = Mathf.CeilToInt(resolution * width);

        DrawLineWeighted(tex, WorldToTexPos(p1), WorldToTexPos(p2),
            weight, dugColor);
        tex.Apply();
    }


    private void Awake()
    {
        // Choose width and height
        width = transform.localScale.x;
        height = transform.localScale.y;
        transform.localScale = new Vector3(1, 1, 1);

        // Terrain
        GenerateTerrain();

        // Setup sprite renderer
        sRenderer = GetComponent<SpriteRenderer>();
        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), resolution);
        sRenderer.sprite = sprite;
        sRenderer.color = Color.white;
    }
    private void GenerateTerrain()
    {
        int pixelsWide = (int)(resolution * width);
        int pixelsHigh = (int)(resolution * height);

        CreateHeightMaps(pixelsWide, pixelsHigh);

        tex = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        DrawInitialTexture();

        tex.Apply();
    }
    private void CreateHeightMaps(int pixelsWide, int pixelsHigh)
    {
        topHeightMap = new int[pixelsWide];
        botHeightMap = new int[pixelsWide];

        for (int i = 0; i < pixelsWide; ++i)
        {
            float t = (float)i / pixelsWide;
            float offset = (Mathf.Sin(t * Mathf.PI * 8f) + 1) * 0.3f;

            topHeightMap[i] = (int)((height - offset) * resolution);
            botHeightMap[i] = (int)(offset * resolution);
        }
    }
    private void DrawInitialTexture()
    {
        for (int x = 0; x < tex.width; ++x)
        {
            for (int y = 0; y < botHeightMap[x]; ++y)
            {
                tex.SetPixel(x, y, dugColor);
            }
            for (int y = botHeightMap[x]; y < topHeightMap[x]; ++y)
            {
                tex.SetPixel(x, y, unDugColor);
            }
            for (int y = topHeightMap[x]; y < tex.height; ++y)
            {
                tex.SetPixel(x, y, dugColor);
            }
        }
    }

    private Vector2 WorldToTexPos(Vector2 worldPos)
    {
        Vector2 ret = worldPos;
        ret -= (Vector2)transform.position;
        ret.x = ((ret.x / width) + 0.5f) * tex.width;
        ret.y = ((ret.y / height) + 0.5f) * tex.height;
        return ret;
    }
    private GroundState GetStateAtTexPos(Vector2 texPos)
    {
        if (texPos.x < 0 || texPos.x >= tex.width || texPos.y < 0 || texPos.y >= tex.height)
        {
            // Out of ground bounds
            return GroundState.None;
        }
        return tex.GetPixel((int)texPos.x, (int)texPos.y) == dugColor ?
            GroundState.Dug : GroundState.UnDug;
    }
    private bool BoundsOverlap(Bounds otherBounds)
    {
        return sRenderer.bounds.Intersects(otherBounds);
    }

    /// <summary>
    /// Does not call tex.apply
    /// </summary>
    /// <param name="texPos"></param>
    private void DigAt(Vector2 texPos)
    {
        tex.SetPixel((int)texPos.x, (int)texPos.y, dugColor);
    }
    /// <summary>
    /// Does not call tex.apply
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void DigAt(int x, int y)
    {
        tex.SetPixel(x, y, dugColor);
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
