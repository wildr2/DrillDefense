using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RockType { None, Any, Dirt, Grass, Gold, Hardrock, Rock3, Rock4 }

public class Ground : MonoBehaviour
{
    private bool initialized = false;
    public int NumRockTypes { get; private set; }

    // Colors
    public Color dirtColor = Color.black;
    public Color grassColor = Color.green;
    public Color goldColor = Color.yellow;
    public Color hardrockColor = Color.black;
    public Color rock3Color = Color.black;
    public Color rock4Color = Color.black;
    private Color clearColor = Color.clear;

    // Generation Parameters
    public const float Width = 32; // world units
    public const float Height = 35; // world units
    public const float Resolution = 15; // pixels per world unit
    private const float GrassHeight = 0.2f; // world units

    // Measurements
    private int pixelsWide, pixelsHigh; // ground units (pixels)
    private int grassPixels;
    // resolution independent value per rock (pixel)
    public static float RockValue = 1f / (Resolution * Resolution); 

    // Rendering
    public SpriteRenderer spriteR;
    private Texture2D tex, texFogData, texDugData;

    // Data
    private RockType[][] data; // rock type for each pixel ([x][y] ground units)
    private byte[] fogData, fogDataInitial, dugData;
    /// <summary>
    /// ideal (fractional) number of pixels (of tex) high (from bottom of image) for each pixel along horizontal
    /// - cast to int for exact exclusive height
    /// </summary>
    private float[] topHeightMap, botHeightMap;
    private float[][] densityMap;

    // Byte data value constants
    private const byte ValFog = 255;
    private const byte ValNoFog = 0;
    private const byte ValDug = 255;
    private const byte ValDugButHidden = 1;
    private const byte ValNotDug = 0;

    // Vision System
    private List<Player> povPlayers = new List<Player>();
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


    // PUBLIC MODIFIERS

    public void Init(int seed)
    {
        Make(seed);
        initialized = true;

        //StartCoroutine(UpdateVisionRoutine());
    }
    /// <summary>
    /// Must be already initialized
    /// </summary>
    /// <param name="player"></param>
    public void SetVisionPOV(Player player)
    {
        povPlayers.Add(player);

        // Remove fog from grassline
        for (int x = 0; x < pixelsWide; ++x)
        {
            if (player.IsTop)
            {
                int topGrassStart = (int)topHeightMap[x] - grassPixels;

                for (int y = topGrassStart; y < (int)topHeightMap[x]; ++y)
                    fogDataInitial[GroundPosToLinIndex(x, y)] = ValNoFog;
            }
            else
            {
                int botGrassStart = (int)botHeightMap[x] + grassPixels;

                for (int y = botGrassStart; y > (int)botHeightMap[x]; --y)
                    fogDataInitial[GroundPosToLinIndex(x, y)] = ValNoFog;
            }
        }
    }
    public void RegisterUnitWithVisionSys(Unit unit)
    {
        if (povPlayers.Contains(unit.Owner))
        {
            unit.SetVisible();
            povUnits.Add(unit);
            unit.onDestroyed += (Unit u) => { povUnits.Remove(u); };
        }
        else
        {
            nonPovUnits.Add(unit);
            unit.onDestroyed += (Unit u) => { nonPovUnits.Remove(u); };
        }
    }
    public int DigPolygon(PolygonCollider2D collider, out int[] rockCounts)
    {
        rockCounts = new int[NumRockTypes];
        int digCount = 0;

        Bounds bounds = collider.bounds;
        Vector2 minGPos = ClampToBounds(WorldToGroundPos(bounds.min));
        Vector2 maxGPos = ClampToBounds(WorldToGroundPos(bounds.max));

        Vector2 center = bounds.center;
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


    // PRIVATE MODIFIERS

    private void Awake()
    {
        NumRockTypes = Tools.EnumLength(typeof(RockType));
    }
    private void Make(int seed)
    {
        Random.InitState(seed);
        GenerateTerrain();
        MakeTextures();
        SetupRenderer();
    }
    private void GenerateTerrain()
    {
        // Measurements
        pixelsWide = (int)(Resolution * Width);
        pixelsHigh = (int)(Resolution * Height);
        grassPixels = (int)(GrassHeight * Resolution);
        int numPixels = pixelsWide * pixelsHigh;

        // Init data
        data = new RockType[pixelsWide][];
        densityMap = new float[pixelsWide][];
        fogDataInitial = new byte[numPixels];
        fogData = new byte[numPixels];
        dugData = new byte[numPixels];

        // Fill data
        CreateHeightMaps(0.2f);
        FillSkyGrassDirt();
        FillRocks(RockType.Rock3, 0.6f, 0.4f, 0.5f, 1f);
        FillRocks(RockType.Gold, 0.55f, 0.35f, 0.15f, 1f);
        FillRocks(RockType.Rock4, 0.7f, 0.5f, 0.5f, 0.5f);
        FillRocks(RockType.Hardrock, 0.55f, 0.25f, 0f, 1f);
        
        FillInitialFog();
    }
    private void CreateHeightMaps(float perlinMove = 0.2f)
    {
        perlinMove /= Resolution;

        topHeightMap = new float[pixelsWide];
        botHeightMap = new float[pixelsWide];

        Vector2 perlinTop = new Vector2(Random.value, Random.value) * 1000f;
        Vector2 perlinBot = new Vector2(Random.value, Random.value) * 1000f;

        for (int x = 0; x < pixelsWide; ++x)
        {
            float offsetTop = Mathf.PerlinNoise(perlinTop.x, perlinTop.y) * 3f;
            float offsetBot = Mathf.PerlinNoise(perlinBot.x, perlinBot.y) * 3f;
            perlinTop.x += perlinMove;
            perlinBot.x += perlinMove;

            topHeightMap[x] = pixelsHigh - 1 - offsetTop * Resolution;
            botHeightMap[x] = offsetBot * Resolution;
        }
    }
    private void FillSkyGrassDirt()
    {
        for (int x = 0; x < pixelsWide; ++x)
        {
            data[x] = new RockType[pixelsHigh];
            densityMap[x] = new float[pixelsHigh];

            int botGrassStart = (int)botHeightMap[x] + grassPixels;
            int topGrassStart = (int)topHeightMap[x] - grassPixels;

            // Sky - clear
            for (int y = 0; y <= (int)botHeightMap[x]; ++y)
                data[x][y] = RockType.None;

            for (int y = (int)topHeightMap[x]; y < pixelsHigh; ++y)
                data[x][y] = RockType.None;

            // Grass
            for (int y = botGrassStart; y > (int)botHeightMap[x]; --y)
                data[x][y] = RockType.Grass;

            for (int y = topGrassStart; y < (int)topHeightMap[x]; ++y)
                data[x][y] = RockType.Grass;

            // Dirt
            for (int y = botGrassStart + 1; y < topGrassStart; ++y)
                data[x][y] = RockType.Dirt;
        }
    }
    private void FillRocks(RockType rock, float perlinThreshold = 0.6f,
        float perlinMove = 0.45f, float depthEffect = 0, float idealDepth = 1)
    {
        perlinMove /= Resolution;

        Vector2 perlinStart = new Vector2(Random.value, Random.value) * 1000f;
        Vector2 perlin = perlinStart;

        for (int x = 0; x < pixelsWide; ++x)
        {
            int botGrassStart = (int)botHeightMap[x] + grassPixels;
            int topGrassStart = (int)topHeightMap[x] - grassPixels;

            for (int y = botGrassStart + 1; y < topGrassStart; ++y)
            {
                float depth = 1 - (Mathf.Abs(y - (pixelsHigh / 2f)) / pixelsHigh) * 2f;
                float idealDepthCloseness = 1 - Mathf.Abs(idealDepth - depth);
                float d = Mathf.Pow(idealDepthCloseness, depthEffect);
                float p = Mathf.PerlinNoise(perlin.x, perlin.y) * d;
                
                if (p > perlinThreshold)
                {
                    // Fill rock here
                    data[x][y] = rock;
                    densityMap[x][y] = (p - perlinThreshold) / (1 - perlinThreshold);
                }

                perlin.y += perlinMove;
            }

            // Update perlin pos
            perlin.x += perlinMove;
            perlin.y = perlinStart.y;
        }
    }
    private void FillInitialFog()
    {
        for (int x = 0; x < pixelsWide; ++x)
        {
            int botGrassStart = (int)botHeightMap[x] + grassPixels;
            int topGrassStart = (int)topHeightMap[x] - grassPixels;

            // Sky - clear
            for (int y = 0; y <= (int)botHeightMap[x]; ++y)
                fogDataInitial[GroundPosToLinIndex(x, y)] = ValNoFog;

            for (int y = (int)topHeightMap[x]; y < pixelsHigh; ++y)
                fogDataInitial[GroundPosToLinIndex(x, y)] = ValNoFog;

            // Everything else
            for (int y = (int)botHeightMap[x] + 1; y < (int)topHeightMap[x]; ++y)
                fogDataInitial[GroundPosToLinIndex(x, y)] = ValFog;
        }
    }
    private void MakeTextures()
    {
        // Ground
        tex = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(ColorsFromData());
        tex.Apply();

        // Fog
        texFogData = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.Alpha8, false);
        texFogData.filterMode = FilterMode.Point;

        // Dug
        texDugData = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.Alpha8, false);
        texDugData.filterMode = FilterMode.Point;
    }
    private Color[] ColorsFromData()
    {
        Color[] colors = new Color[pixelsWide * pixelsHigh];

        int i = 0;
        for (int y = 0; y < pixelsHigh; ++y)
        {
            for (int x = 0; x < pixelsWide; ++x)
            {
                RockType rock = data[x][y];
                switch (rock)
                {
                    case RockType.Dirt:
                        colors[i] = dirtColor;
                        break;

                    case RockType.Gold:
                        colors[i] = Color.Lerp(dirtColor, goldColor,
                            0.5f + (int)(densityMap[x][y] * 5) / 5f);
                        break;

                    case RockType.Hardrock:
                        colors[i] = Color.Lerp(dirtColor, hardrockColor,
                            0.7f + (int)(densityMap[x][y] * 3) / 3f);
                        break;

                    case RockType.Rock3:
                        colors[i] = Color.Lerp(dirtColor, rock3Color,
                            0.5f + (int)(densityMap[x][y] * 3) / 3f);
                        break;

                    case RockType.Rock4:
                        colors[i] = Color.Lerp(rock4Color, Color.white,
                            (int)(densityMap[x][y] * 3) / 3f * 0.7f);
                        break;

                    case RockType.None:
                        colors[i] = clearColor;
                        break;

                    case RockType.Grass:
                        colors[i] = grassColor;
                        break;
                }
                ++i;
            }
        }

        return colors;
    }
    private void SetupRenderer()
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
        if (VisionAt(i))
            dugData[i] = ValDug;
        else
            dugData[i] = ValDugButHidden;
    }

    //private IEnumerator UpdateVisionRoutine()
    //{
    //    while (true)
    //    {
    //        UpdateVision();
    //        yield return new WaitForSeconds(0.1f);
    //    }
    //}
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
            bool inXbounds = pos.x >= 0 && pos.x < pixelsWide;
            bool inYbounds = pos.y >= 0 && pos.y < pixelsHigh;

            if (!inYbounds || (inXbounds && VisionAt(GroundPosToLinIndex(pos))))
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
    
    private bool VisionAt(int groundLinIndex)
    {
        return fogData[groundLinIndex] == 0;
    }

    private int GroundPosToLinIndex(Vector2 groundPos)
    {
        return (int)groundPos.y * pixelsWide + (int)groundPos.x;
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
    private Vector2 ClampToBounds(Vector2 groundPos)
    {
        groundPos.x = Mathf.Min(Mathf.Max(0, groundPos.x), pixelsWide-1);
        groundPos.y = Mathf.Min(Mathf.Max(0, groundPos.y), pixelsHigh - 1);
        return groundPos;
    }


    // STATIC HELPERS

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