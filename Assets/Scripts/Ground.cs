using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GroundState { UnDug, Dug, None }

public class Ground : MonoBehaviour
{
    public Color dirtColor = Color.black;
    public Color grassColor = Color.green;
    private Color clearColor = Color.clear;

    public SpriteRenderer spriteR, spriteRBG;
    private Sprite sprite;
    private Texture2D tex, texBG;

    // ideal (fractional) number of pixels high (from bottom of image) for each pixel along horizontal
    private float[] topHeightMap, botHeightMap; 

    public float Width { get; private set; } // world units
    public float Height { get; private set; } // world units
    private float resolution = 15; // pixels per world unit
    private float grassHeight = 0.2f; // world units
    private float bgDarkness = 0.3f;
    


    public float GetHeightAt(float worldPosX, bool top)
    {
        // Check out of bounds
        if (worldPosX >= spriteR.bounds.max.x ||
            worldPosX < spriteR.bounds.min.x)
        {
            return 0;
        }

        // To tex x pos
        float x = worldPosX - transform.position.x;
        x = ((x / Width) + 0.5f) * tex.width;

        // Tex y pos
        float y = top ? topHeightMap[(int)x] : botHeightMap[(int)x];

        // To world y pos
        return ((y / tex.height) - 0.5f) * Height + transform.position.y;
    }
    public Vector2 GetNormalAt(float worldPosX, bool top)
    {
        // Check out of bounds
        if (worldPosX >= spriteR.bounds.max.x ||
            worldPosX < spriteR.bounds.min.x)
        {
            return Vector2.zero;
        }

        // To tex x pos
        float x = worldPosX - transform.position.x;
        x = ((x / Width) + 0.5f) * tex.width;

        // Nearby tex y positions
        x = Mathf.Min(x, topHeightMap.Length - 2);
        float x2 = x + 1;
        float y = top ? topHeightMap[(int)x] : botHeightMap[(int)x];
        float y2 = top ? topHeightMap[(int)x2] : botHeightMap[(int)x2];

        // Normal
        Vector2 tangent = new Vector2(x2 - x, y2 - y);
        Vector2 n = Vector3.Cross(Vector3.forward, tangent).normalized;
        return top ? n : -n;
    }
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
    public void DrillLine(Vector2 p1, Vector2 p2, float Width)
    {
        int weight = Mathf.CeilToInt(resolution * Width);

        DrawLineWeighted(tex, WorldToTexPos(p1), WorldToTexPos(p2),
            weight, clearColor);
        tex.Apply();
    }


    private void Awake()
    {
        // Choose Width and Height
        Width = transform.localScale.x;
        Height = transform.localScale.y;
        transform.localScale = new Vector3(1, 1, 1);

        // Generate
        MakeTerrain();
        SetupSprites();
    }
    private void MakeTerrain()
    {
        int pixelsWide = (int)(resolution * Width);
        int pixelsHigh = (int)(resolution * Height);

        CreateHeightMaps(pixelsWide, pixelsHigh);

        tex = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        DrawInitialTexture();
        tex.Apply();

        MakeTexBG();
    }
    private void CreateHeightMaps(int pixelsWide, int pixelsHigh)
    {
        topHeightMap = new float[pixelsWide];
        botHeightMap = new float[pixelsWide];

        for (int i = 0; i < pixelsWide; ++i)
        {
            float t = (float)i / pixelsWide;
            float offset = (Mathf.Sin(t * Mathf.PI * 8f) + 1) * 0.3f;

            topHeightMap[i] = (Height - offset) * resolution;
            botHeightMap[i] = offset * resolution;
        }
    }
    private void DrawInitialTexture()
    {
        int grassPixels = (int)(grassHeight * resolution);

        for (int x = 0; x < tex.width; ++x)
        {
            int botGrassEnd = (int)botHeightMap[x] + grassPixels;
            int topGrassStart = (int)topHeightMap[x] - grassPixels;

            for (int y = 0; y < botHeightMap[x]; ++y) // clear
                tex.SetPixel(x, y, clearColor);
            for (int y = (int)botHeightMap[x]; y < botGrassEnd; ++y) // grass
                tex.SetPixel(x, y, grassColor);
            for (int y = botGrassEnd; y < topGrassStart; ++y) // dirt
                tex.SetPixel(x, y, dirtColor);
            for (int y = topGrassStart; y < topHeightMap[x]; ++y) // grass
                tex.SetPixel(x, y, grassColor);
            for (int y = (int)topHeightMap[x]; y < tex.height; ++y) // clear
                tex.SetPixel(x, y, clearColor);
        }
    }
    private void MakeTexBG()
    {
        texBG = new Texture2D(tex.width, tex.height);
        texBG.filterMode = FilterMode.Point;

        Color[] colors = tex.GetPixels();
        for (int i = 0; i < colors.Length; ++i)
        {
            if (colors[i].a > 0)
                colors[i] = Color.Lerp(colors[i], Color.black, bgDarkness);
        }
        texBG.SetPixels(colors);
        texBG.Apply();
    }

    private void SetupSprites()
    {
        // Foreground
        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), resolution);
        spriteR.sprite = sprite;
        spriteR.color = Color.white;

        // Background
        spriteRBG.sprite = Sprite.Create(texBG, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), resolution);
        spriteRBG.color = Color.white;
    }

    private Vector2 WorldToTexPos(Vector2 worldPos)
    {
        Vector2 ret = worldPos;
        ret -= (Vector2)transform.position;
        ret.x = ((ret.x / Width) + 0.5f) * tex.width;
        ret.y = ((ret.y / Height) + 0.5f) * tex.height;
        return ret;
    }
    private GroundState GetStateAtTexPos(Vector2 texPos)
    {
        if (texPos.x < 0 || texPos.x >= tex.width || texPos.y < 0 || texPos.y >= tex.height)
        {
            // Out of ground bounds
            return GroundState.None;
        }
        return tex.GetPixel((int)texPos.x, (int)texPos.y) == clearColor ?
            GroundState.Dug : GroundState.UnDug;
    }
    private bool BoundsOverlap(Bounds otherBounds)
    {
        return spriteR.bounds.Intersects(otherBounds);
    }

    /// <summary>
    /// Does not call tex.apply
    /// </summary>
    /// <param name="texPos"></param>
    private void DigAt(Vector2 texPos)
    {
        tex.SetPixel((int)texPos.x, (int)texPos.y, clearColor);
    }
    /// <summary>
    /// Does not call tex.apply
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void DigAt(int x, int y)
    {
        tex.SetPixel(x, y, clearColor);
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
