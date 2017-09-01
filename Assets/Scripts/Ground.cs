using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RockType { None, Any, Dirt, Grass, Gold, Hardrock }

public class Ground : MonoBehaviour
{
    public SeedManager seeder;
    private bool initialized = false;
    public int NumRockTypes { get; private set; }

    // Colors
    public Color dirtColor = Color.black;
    public Color grassColor = Color.green;
    public Color goldColor = Color.yellow;
    public Color hardrockColor = Color.black;
    private Color clearColor = Color.clear;

    // Generation Parameters
    public const float Resolution = 15; // pixels per world unit
    private const float GrassHeight = 0.2f; // world units

    // Measurements
    public float Width { get; private set; } // world units
    public float Height { get; private set; } // world units
    private int pixelsWide, pixelsHigh; // ground units (pixels)
    private int grassPixels;

    // Rendering
    public SpriteRenderer spriteR;
    private Texture2D tex, texFogData, texDugData;

    // Data
    private RockType[][] data; // rock type for each pixel ([x][y] ground units)
    private byte[] fogData, fogDataInitial, dugData;
    /// <summary>
    /// ideal (fractional) number of pixels (of tex) high (from bottom of image) for each pixel along horizontal
    /// </summary>
    private float[] topHeightMap, botHeightMap;
    private float[][] densityMap;

    // Byte data value constants
    private const byte ValFog = 255;
    private const byte ValNeverFog = 1;
    private const byte ValNoFog = 0;
    private const byte ValDug = 255;
    private const byte ValDugButHidden = 1;
    private const byte ValNotDug = 0;

    // Vision System
    private List<Unit> povUnits = new List<Unit>();
    private List<Unit> nonPovUnits = new List<Unit>();



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
    public int DigPolygon(PolygonCollider2D collider, out int[] rockCounts)
    {
        rockCounts = new int[NumRockTypes];
        int digCount = 0;

        Bounds bounds = collider.bounds;
        Vector2 minGPos = WorldToGroundPos(bounds.min);
        Vector2 maxGPos = WorldToGroundPos(bounds.max);

        Vector2 wp = bounds.min;
        float deltaWp = 1f / Resolution;
        float wy0 = wp.y;

        for (int x = (int)minGPos.x; x <= (int)maxGPos.x; ++x)
        {
            for (int y = (int)minGPos.y; y <= (int)maxGPos.y; ++y)
            {
                if (collider.OverlapPoint(wp))
                {
                    RockType rock = data[x][y];
                    rockCounts[(int)rock] += 1;
                    if (rock != RockType.None)
                    {
                        DigAt(x, y);
                        ++digCount;
                    }   
                }
                wp.y += deltaWp;
            }
            wp.x += deltaWp;
            wp.y = wy0;
        }

        return digCount;
    }
    public void DrillLine(Vector2 p1, Vector2 p2, float Width)
    {
        int weight = Mathf.CeilToInt(Resolution * Width);

        DrawLineWeighted(tex, WorldToGroundPos(p1), WorldToGroundPos(p2),
            weight, clearColor);
    }


    // PRIVATE MODIFIERS

    private void Awake()
    {
        NumRockTypes = Tools.EnumLength(typeof(RockType));

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

        //StartCoroutine(UpdateVision());
    }
    private void MakeTerrain()
    {
        pixelsWide = (int)(Resolution * Width);
        pixelsHigh = (int)(Resolution * Height);
        grassPixels = (int)(GrassHeight * Resolution);

        CreateHeightMaps();

        // Fill Data
        data = new RockType[pixelsWide][];
        densityMap = new float[pixelsWide][];

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

            topHeightMap[i] = pixelsHigh - 1 - offsetTop * Resolution;
            botHeightMap[i] = offsetBot * Resolution;
        }
    }
    private void MakeTextures()
    {
        // Get data
        int numPixels = pixelsWide * pixelsHigh;
        Color[] colors = new Color[numPixels];
        fogDataInitial = new byte[numPixels];
        fogData = new byte[numPixels];
        dugData = new byte[numPixels];
        

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

                if (y > topHeightMap[x] - 1 - grassPixels || y < botHeightMap[x] - 1)
                    fogDataInitial[i] = ValNeverFog;
                else
                    fogDataInitial[i] = ValFog;

                ++i;
            }
        }

        // Create Texture2Ds
        tex = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(colors);
        tex.Apply();
        texFogData = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.Alpha8, false);
        texFogData.filterMode = FilterMode.Point;
        texDugData = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.Alpha8, false);
        texDugData.filterMode = FilterMode.Point;
    }
    private void MakeSprites()
    {
        spriteR.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), Resolution);
        spriteR.material.SetTexture("_FogTex", texFogData);
        spriteR.material.SetTexture("_DugTex", texDugData);
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        texDugData.LoadRawTextureData(dugData);
        texDugData.Apply();
        UpdateVision();
    }

    private void DigAt(Vector2 groundPos)
    {
        DigAt((int)groundPos.x, (int)groundPos.y);
    }
    private void DigAt(int x, int y)
    {
        data[x][y] = RockType.None;

        int i = GroundPosToLinIndex(x, y);
        if (VisionAt(x, y))
            dugData[i] = ValDug;
        else
            dugData[i] = ValDugButHidden;
    }

    private IEnumerator UpdateVisionRoutine()
    {
        while (true)
        {
            

            yield return new WaitForSeconds(0.1f);
        }
    }
    private void UpdateVision()
    {
        // Reset fog data
        System.Buffer.BlockCopy(fogDataInitial, 0, fogData, 0, fogData.Length);

        // Apply unit vision to fog data
        foreach (Unit unit in povUnits)
        {
            ApplyVisionCircle(unit.transform.position, unit.VisionRadius);
        }

        // Apply fog data texture changes
        texFogData.LoadRawTextureData(fogData);
        texFogData.Apply();

        // Update other unit visibility
        for (int i = 0; i < nonPovUnits.Count; ++i)
        {
            Unit unit = nonPovUnits[i];
            Vector2 pos = WorldToGroundPos(unit.transform.position);
            if (InBounds(pos) && VisionAt(pos))
            {
                unit.SetVisible(true);
                if (unit.GetComponent<Building>() != null)
                {
                    // Buildings (unmoving units) stay visible once seen
                    nonPovUnits.Remove(unit);
                    --i;
                }
            }
            else
            {
                unit.SetVisible(false);
            }
        }
    }
    private void ApplyVisionCircle(Vector2 worldPos, float radius)
    {
        IVector2 origin = new IVector2(WorldToGroundPos(worldPos));
        int r = (int)(radius * Resolution);

        float time = Time.time;
        int pixelsN = fogData.Length;


        for (int y = -r; y < r; ++y)
        {
            float rFactor = 0.8f + 0.2f * Mathf.PerlinNoise(0, time * 0.7f + y * 0.04f);
            int len = (int)Mathf.Sqrt(r * r * rFactor - y * y);
            int gy = y + origin.y;
            if (gy < 0 || gy >= pixelsHigh) continue;

            for (int x = -len; x < len; ++x)
            {
                int gx = x + origin.x;
                if (gx < 0 || gx >= pixelsWide) continue;
                int i = gy * pixelsWide + gx;

                // Has vision
                fogData[i] = ValNoFog;
                if (dugData[i] == ValDugButHidden)
                {
                    // Fog was hiding dug terrain that is now visible
                    dugData[i] = ValDug;
                }
            }

        }
    }


    // PRIVATE ACCESSORS
    
    private bool VisionAt(Vector2 groundPos)
    {
        return VisionAt((int)groundPos.x, (int)groundPos.y);
    }
    private bool VisionAt(int groundX, int groundY)
    {
        return fogData[GroundPosToLinIndex(groundX, groundY)] < 10;
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
    private Vector2 WorldToGroundPos(Vector2 pos)
    {
        pos -= (Vector2)transform.position;
        pos.x = ((pos.x / Width) + 0.5f) * pixelsWide;
        pos.y = ((pos.y / Height) + 0.5f) * pixelsHigh;
        return pos;
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
    private bool InBounds(Vector2 groundPos)
    {
        return InBounds((int)groundPos.x, (int)groundPos.y);
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

    private static bool SameSideOfLine(Vector2 p1, Vector2 p2, Vector2 a, Vector2 b)
    {
        Vector3 cp1 = Vector3.Cross(b - a, p1 - a);
        Vector3 cp2 = Vector3.Cross(b - a, p2 - a);
        return Vector3.Dot(cp1, cp2) >= 0;
    }
    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        return SameSideOfLine(p, a, b, c) && SameSideOfLine(p, b, a, c) &&
            SameSideOfLine(p, c, a, b);
    }
}
