using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RockType { None, Any, Dirt, Grass, Gold, Hardrock }

public class Ground : MonoBehaviour
{
    public SeedManager seeder;
    private bool initialized = false;

    // Colors
    public Color dirtColor = Color.black;
    public Color grassColor = Color.green;
    public Color goldColor = Color.yellow;
    public Color hardrockColor = Color.black;
    private Color clearColor = Color.clear;

    // Rendering
    public SpriteRenderer spriteR, spriteRBG, spriteRFog;
    private Texture2D tex, texBG, texFog;

    // Measurements
    public float Width { get; private set; } // world units
    public float Height { get; private set; } // world units
    private int pixelsWide, pixelsHigh; // ground units (pixels)
    private int grassPixels;
    public const float resolution = 15; // pixels per world unit
    private const float grassHeight = 0.2f; // world units

    // Data
    private RockType[][] data; // rock type for each pixel ([x][y] ground units)
    /// <summary>
    /// ideal (fractional) number of pixels (of tex) high (from bottom of image) for each pixel along horizontal
    /// </summary>
    private float[] topHeightMap, botHeightMap;
    private float[][] densityMap;

    // Vision
    private List<Unit> povUnits = new List<Unit>();
    private List<Unit> nonPovUnits = new List<Unit>();
    private byte[] fogPixels;
    private bool[][] dugButFogged;


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

    public void RegisterUnitWithVisionSys(Unit unit)
    {
        if (unit.Owner.ClientPOV)
        {
            povUnits.Add(unit);
            unit.onDestroyed += (Unit u) => { povUnits.Remove(u); };
        }
        else
        {
            nonPovUnits.Add(unit);
            unit.onDestroyed += (Unit u) => { nonPovUnits.Remove(u); };
        }
    }

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

        return dugAny;
    }
    public void DrillLine(Vector2 p1, Vector2 p2, float Width)
    {
        int weight = Mathf.CeilToInt(resolution * Width);

        DrawLineWeighted(tex, WorldToGroundPos(p1), WorldToGroundPos(p2),
            weight, clearColor);
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
        initialized = true;

        StartCoroutine(UpdateVision());
    }
    private void MakeTerrain()
    {
        pixelsWide = (int)(resolution * Width);
        pixelsHigh = (int)(resolution * Height);
        grassPixels = (int)(grassHeight * resolution);

        CreateHeightMaps();

        // Fill Data
        data = new RockType[pixelsWide][];
        densityMap = new float[pixelsWide][];
        dugButFogged = new bool[pixelsWide][];

        Vector2 perlinGoldStart = new Vector2(Random.value, Random.value) * 1000f;
        Vector2 perlinGold = perlinGoldStart;
        float perlinGoldThreshold = 0.6f;

        Vector2 perlinHardStart = new Vector2(Random.value, Random.value) * 1000f;
        Vector2 perlinHard = perlinHardStart;
        float perlinHardThreshold = 0.6f;

        for (int x = 0; x < pixelsWide; ++x)
        {
            data[x] = new RockType[pixelsHigh];
            densityMap[x] = new float[pixelsHigh];
            dugButFogged[x] = new bool[pixelsHigh];

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
                {
                    data[x][y] = RockType.Hardrock; // hardrock
                    densityMap[x][y] = (Mathf.PerlinNoise(perlinHard.x, perlinHard.y) - perlinHardThreshold)
                        / perlinHardThreshold;
                }   
                else if (Mathf.PerlinNoise(perlinGold.x, perlinGold.y) > perlinGoldThreshold)
                {
                    data[x][y] = RockType.Gold; // gold
                    densityMap[x][y] = (Mathf.PerlinNoise(perlinGold.x, perlinGold.y) - perlinGoldThreshold)
                        / perlinGoldThreshold;
                }
                else
                {
                    data[x][y] = RockType.Dirt; // dirt
                }    

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

        Vector2 perlinTop = new Vector2(Random.value, Random.value) * 1000f;
        Vector2 perlinBot = new Vector2(Random.value, Random.value) * 1000f;

        for (int i = 0; i < pixelsWide; ++i)
        {
            float offsetTop = Mathf.PerlinNoise(perlinTop.x, perlinTop.y) * 3f; //(Mathf.Sin(t * Mathf.PI * 8f) + 1) * 0.3f;
            float offsetBot = Mathf.PerlinNoise(perlinBot.x, perlinBot.y) * 3f; //(Mathf.Sin(t * Mathf.PI * 8f) + 1) * 0.3f;
            perlinTop.x += 0.013f;
            perlinBot.x += 0.013f;

            topHeightMap[i] = (Height - offsetTop) * resolution;
            botHeightMap[i] = offsetBot * resolution;
        }
    }
    private void MakeTextures()
    {
        tex = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        texBG = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.RGBA32, false);
        texBG.filterMode = FilterMode.Point;
        texFog = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.Alpha8, false);
        texFog.filterMode = FilterMode.Point;

        // FIll texture from data
        int numPixels = pixelsWide * pixelsHigh;
        Color[] colors = new Color[numPixels];
        Color[] colorsBG = new Color[numPixels];
        fogPixels = new byte[numPixels];
        int i = 0;

        for (int y = 0; y < pixelsHigh; ++y)
        {
            for (int x = 0; x < pixelsWide; ++x)
            {
                RockType rock = data[x][y];
                colors[i] = rock == RockType.None ? clearColor :
                            rock == RockType.Dirt ? dirtColor :
                            rock == RockType.Gold ? Color.Lerp(dirtColor, goldColor, 0.5f + (int)(densityMap[x][y] * 5) / 5f) :
                            rock == RockType.Grass ? grassColor :
                            rock == RockType.Hardrock ? Color.Lerp(dirtColor, hardrockColor, 0.5f + (int)(densityMap[x][y] * 5) / 5f) 
                            : Color.red;

                colorsBG[i] = rock == RockType.None ? clearColor :
                    Color.Lerp(colors[i], dirtColor, 0.8f);

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
        spriteR.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), resolution);
        spriteR.color = Color.white;

        // Background
        spriteRBG.sprite = Sprite.Create(texBG, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), resolution);

        // Fog
        spriteRFog.sprite = Sprite.Create(texFog, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), resolution);
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        tex.Apply();
    }

    private void DigAt(Vector2 groundPos)
    {
        DigAt((int)groundPos.x, (int)groundPos.y);
    }
    private void DigAt(int x, int y)
    {
        data[x][y] = RockType.None;
        if (VisionAt(x, y))
        {
            tex.SetPixel(x, y, clearColor);
        }
        else
        {
            dugButFogged[x][y] = true;
        }
    }

    private IEnumerator UpdateVision()
    {
        while (true)
        {
            // Clear fog texture
            for (int i = 0; i < fogPixels.Length; ++i)
            {
                //Vector2 pos = LinIndexToGroundPos(i);
                //bool sky = pos.y < botHeightMap[(int)pos.x] - 1
                //        && pos.y > topHeightMap[(int)pos.x] - grassPixels - 1;

                //fogPixels[i] = sky ? Color.clear : Color.white;
                fogPixels[i] = 255;
            }

            // Clear unit vision to fog texture
            foreach (Unit unit in povUnits)
            {
                ApplyVisionCircle(unit.transform.position, unit.VisionRadius);
            }

            // Apply fog texture changes
            texFog.LoadRawTextureData(fogPixels);
            texFog.Apply();

            // Update other unit visibility
            foreach (Unit unit in nonPovUnits)
            {
                Vector2 pos = WorldToGroundPos(unit.transform.position);
                unit.SetVisible(VisionAt(pos));
            }

            //yield return null;
            yield return new WaitForSeconds(0.1f);
        }
    }
    private void ApplyVisionCircle(Vector2 worldPos, float radius)
    {
        IVector2 origin = new IVector2(WorldToGroundPos(worldPos));
        int r = (int)(radius * resolution);

        //int x0 = Mathf.Max(0, origin.x - r);
        //if (x0 >= pixelsWide) return;
        //int x1 = Mathf.Min(pixelsWide-1, origin.x + r);
        //if (x1 < 0) return;

        //int y0 = Mathf.Max(0, origin.y - r);
        //if (y0 >= pixelsHigh) return;
        //int y1 = Mathf.Min(pixelsHigh-1, origin.y + r);
        //if (y1 < 0) return;

        float time = Time.time;
        int pixelsN = fogPixels.Length;


        for (int y = -r; y < r; ++y)
        {
            float rFactor = 0.8f + 0.2f * Mathf.PerlinNoise(0, time * 0.7f + y * 0.04f);
            int len = (int)Mathf.Sqrt(r * r * rFactor - y * y);
            int gy = y + origin.y;

            for (int x = -len; x < len; ++x)
            {
                int gx = x + origin.x;
                int i = gy * pixelsWide + gx;
                if (i < 0 || i >= pixelsN) continue;

                // Has vision
                fogPixels[i] = 0;

                if (dugButFogged[gx][gy])
                {
                    // Fog was hiding dug terrain that is now visible
                    tex.SetPixel(gx, gy, clearColor);
                    dugButFogged[gx][gy] = false;
                }
            }

        }

        //for (int y = y0; y <= y1; ++y)
        //{
        //    int i = y * pixelsWide + x0;
        //    int relY = y - origin.y;
        //    float rFactor = 0.8f + 0.2f * Mathf.PerlinNoise(0, time * 0.7f + relY * 0.06f);


        //    for (int x = x0; x <= x1; ++x)
        //    {
        //        int relX = x - origin.x;
        //        if (relX * relX + relY * relY <= r * r * rFactor)
        //        {
        //            // Has vision
        //            fogPixels[i] = 0;

        //            if (dugButFogged[x][y])
        //            {
        //                // Fog was hiding dug terrain that is now visible
        //                tex.SetPixel(x, y, clearColor);
        //                dugButFogged[x][y] = false;
        //            }
        //        }
        //        ++i;
        //    }
        //}
    }


    // PRIVATE ACCESSORS
    
    private bool VisionAt(Vector2 groundPos)
    {
        return texFog.GetPixel((int)groundPos.x, (int)groundPos.y).a == 0;
    }
    private bool VisionAt(int groundX, int groundY)
    {
        return texFog.GetPixel(groundX, groundX).a == 0;
    }

    private int GroundPosToLinIndex(Vector2 groundPos)
    {
        return (int)(groundPos.y * pixelsWide + groundPos.x);
    }
    private int GroundPosToLinIndex(int x, int y)
    {
        return y * pixelsWide + x;
    }
    private Vector2 LinIndexToGroundPos(int linIndex)
    {
        return new Vector2(linIndex % pixelsWide,
            Mathf.FloorToInt((float)linIndex / pixelsWide));
    }
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
    private bool InBounds(int groundX, int groundY)
    {
        return groundX >= 0 && groundX < data.Length && groundY >= 0 && groundY < data[0].Length;
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
