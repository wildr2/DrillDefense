using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RockType { None, Any, Dirt, Grass, Gold, Hardrock }

public class Ground : MonoBehaviour
{
    public SeedManager seeder;

    // Colors
    public Color dirtColor = Color.black;
    public Color grassColor = Color.green;
    public Color goldColor = Color.yellow;
    public Color hardrockColor = Color.black;
    private Color clearColor = Color.clear;
    public Color dugTint = Color.black;

    // Rendering
    public SpriteRenderer spriteR, spriteRBG;
    private Sprite sprite;
    private Texture2D tex, texBG;

    // Measurements
    public float Width { get; private set; } // world units
    public float Height { get; private set; } // world units
    private int pixelsWide, pixelsHigh; // ground units (pixels)
    public const float resolution = 15; // pixels per world unit
    private const float grassHeight = 0.2f; // world units
    //private const float bgDarkness = 0.6f;

    // Data
    private RockType[][] data; // rock type for each pixel ([x][y] ground units)
    /// <summary>
    /// ideal (fractional) number of pixels (of tex) high (from bottom of image) for each pixel along horizontal
    /// </summary>
    private float[] topHeightMap, botHeightMap;
    

    // PUBLIC ACCESSORS

    public float GetHeightAt(float worldPosX, bool top)
    {
        // Check out of bounds
        if (worldPosX >= spriteR.bounds.max.x ||
            worldPosX < spriteR.bounds.min.x)
        {
            return 0;
        }

        // To ground x pos
        float x = worldPosX - transform.position.x;
        x = ((x / Width) + 0.5f) * pixelsWide;

        // Ground y pos
        float y = top ? topHeightMap[(int)x] : botHeightMap[(int)x];

        // To world y pos
        return ((y / pixelsHigh) - 0.5f) * Height + transform.position.y;
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
        x = ((x / Width) + 0.5f) * pixelsWide;

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
    public RockType GetRockTypeAt(Vector2 worldPos)
    {
        return GetRockTypeAtGPos(WorldToGroundPos(worldPos));
    }
    /// <summary>
    /// Return whether any non-transparent pixels in a sprite overlap ground in specified state
    /// </summary>
    /// <param name="tex"></param>
    /// <returns></returns>
    public bool SpriteOverlaps(SpriteRenderer sr, RockType state = RockType.Any)
    {
        // TODO: interpolate groundPos instead of worldPos to avoid calls to WorldToGroundPos

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
                    RockType rock = GetRockTypeAt(pos);
                    if ((state == RockType.Any && rock != RockType.None) || rock == state)
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


    // PUBLIC MODIFIERS

    /// <summary>
    /// Returns whether any ground was dug successfully
    /// </summary>
    /// <param name="sr"></param>
    /// <returns></returns>
    public bool DigWithSprite(SpriteRenderer sr, out Dictionary<RockType, int> digCount)
    {
        // TODO: interpolate groundPos instead of worldPos to avoid calls to WorldToGroundPos

        digCount = new Dictionary<RockType, int>();
        foreach (RockType rock in Tools.EnumValues(typeof(RockType)))
        {
            digCount[rock] = 0;
        }


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
                    Vector2 groundPos = WorldToGroundPos(pos);
                    RockType rock = GetRockTypeAtGPos(groundPos);
                    digCount[rock] += 1;
                    if (rock != RockType.None)
                    {
                        // Dig
                        DigAt(groundPos);
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

        DrawLineWeighted(tex, WorldToGroundPos(p1), WorldToGroundPos(p2),
            weight, clearColor);
        tex.Apply();
    }


    // PRIVATE MODIFIERS

    private void Awake()
    {
        // Determine width and height (world units)
        Width = transform.localScale.x;
        Height = transform.localScale.y;
        transform.localScale = new Vector3(1, 1, 1);

        // Gererate after synched random seed is known
        seeder.onSeedSet += Make;
    }
    private void Make(int seed)
    {
        Random.InitState(seed);
        MakeTerrain();
        MakeTextures();
        MakeSprites();
    }
    private void MakeTerrain()
    {
        pixelsWide = (int)(resolution * Width);
        pixelsHigh = (int)(resolution * Height);
        int grassPixels = (int)(grassHeight * resolution);

        CreateHeightMaps();

        // Fill Data
        data = new RockType[pixelsWide][];

        Vector2 perlinGoldStart = (Vector2)Random.onUnitSphere * Random.value * 1000f;
        Vector2 perlinGold = perlinGoldStart;
        float perlinGoldThreshold = 0.6f;

        Vector2 perlinHardStart = (Vector2)Random.onUnitSphere * Random.value * 1000f;
        Vector2 perlinHard = perlinHardStart;
        float perlinHardThreshold = 0.6f;

        for (int x = 0; x < pixelsWide; ++x)
        {
            data[x] = new RockType[pixelsHigh];

            int botGrassEnd = (int)botHeightMap[x] + grassPixels;
            int topGrassStart = (int)topHeightMap[x] - grassPixels;

            // Clear Space
            for (int y = 0; y < botHeightMap[x]; ++y)
                data[x][y] = RockType.None;

            for (int y = (int)topHeightMap[x]; y < pixelsHigh; ++y)
                data[x][y] = RockType.None;

            // Grass
            for (int y = (int)botHeightMap[x]; y < botGrassEnd; ++y)
                data[x][y] = RockType.Grass;

            for (int y = topGrassStart; y < topHeightMap[x]; ++y)
                data[x][y] = RockType.Grass;

            // Dirt, Gold, Hardrock
            for (int y = botGrassEnd; y < topGrassStart; ++y)
            {
                if (Mathf.PerlinNoise(perlinHard.x, perlinHard.y) > perlinHardThreshold)
                    data[x][y] = RockType.Hardrock; // hardrock
                else if (Mathf.PerlinNoise(perlinGold.x, perlinGold.y) > perlinGoldThreshold)
                    data[x][y] = RockType.Gold; // gold
                else
                    data[x][y] = RockType.Dirt; // dirt

                perlinGold.y += 0.03f;
                perlinHard.y += 0.02f;
            }

            perlinGold.x += 0.03f;
            perlinGold.y = perlinGoldStart.y;
            perlinHard.x += 0.02f;
            perlinHard.y = perlinHardStart.y;
        }
    }
    private void CreateHeightMaps()
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
    private void MakeTextures()
    {
        tex = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        texBG = new Texture2D(tex.width, tex.height);
        texBG.filterMode = FilterMode.Point;

        // FIll texture from data
        int numPixels = pixelsWide * pixelsHigh;
        Color[] colors = new Color[numPixels];
        Color[] colorsBG = new Color[numPixels];
        int i = 0;

        for (int y = 0; y < pixelsHigh; ++y)
        {
            for (int x = 0; x < pixelsWide; ++x)
            {
                RockType rock = data[x][y];
                colors[i] = rock == RockType.None ? clearColor :
                            rock == RockType.Dirt ? dirtColor :
                            rock == RockType.Gold ? goldColor :
                            rock == RockType.Grass ? grassColor :
                            rock == RockType.Hardrock ? hardrockColor : Color.red;

                colorsBG[i] = rock == RockType.None ? clearColor :
                    Color.Lerp(Color.Lerp(colors[i], dirtColor, 0.4f), dugTint, 0.8f);

                ++i;
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        texBG.SetPixels(colorsBG);
        texBG.Apply();
    }
    private void MakeSprites()
    {
        // Foreground
        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), resolution);
        spriteR.sprite = sprite;
        spriteR.color = Color.white;

        // Background
        spriteRBG.sprite = Sprite.Create(texBG, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), resolution);
        spriteRBG.color = Color.white;
    }

    private void DigAt(Vector2 groundPos)
    {
        DigAt((int)groundPos.x, (int)groundPos.y);
    }
    private void DigAt(int x, int y)
    {
        data[x][y] = RockType.None;
        tex.SetPixel(x, y, clearColor);
    }


    // PRIVATE ACCESSORS
    
    private Vector2 WorldToGroundPos(Vector2 worldPos)
    {
        Vector2 ret = worldPos;
        ret -= (Vector2)transform.position;
        ret.x = ((ret.x / Width) + 0.5f) * pixelsWide;
        ret.y = ((ret.y / Height) + 0.5f) * pixelsHigh;
        return ret;
    }
    private RockType GetRockTypeAtGPos(Vector2 groundPos)
    {
        return GetRockTypeAtGPos((int)groundPos.x, (int)groundPos.y);
    }
    private RockType GetRockTypeAtGPos(int groundX, int groundY)
    {
        if (groundX < 0 || groundX >= data.Length || groundY < 0 || groundY >= data[0].Length)
        {
            // Out of ground bounds
            return RockType.None;
        }
        return data[groundX][groundY];
    }

    private bool BoundsOverlap(Bounds otherBounds)
    {
        return spriteR.bounds.Intersects(otherBounds);
    }


    // STATIC HELPERS

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
