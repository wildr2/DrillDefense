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
    public Color topGrassColor = Color.black;
    public Color botGrassColor = Color.black;
    public Color goldColor = Color.yellow;
    public Color hardrockColor = Color.black;
    public Color rock3Color = Color.black;
    public Color rock4Color = Color.black;
    private Color clearColor = Color.clear;

    // Generation Parameters
    public const float Width = 32; // world units
    public const float Height = 35; // world units
    public const int Resolution = 50; // pixels per world unit
    private const int CollectResolution = 5; // squares per world unit
    private const float GrassHeight = 0.2f; // world units

    // Measurements
    private int pixelsWide, pixelsHigh; // ground units (pixels)
    private int cRocksWide, cRocksHigh; // ground units (pixels)
    private int grassPixels;
    // resolution independent value per rock (pixel)
    public static float RockValue = 1f / (CollectResolution * CollectResolution); 

    // Rendering
    public SpriteRenderer spriteR;
    private Texture2D tex;
    public Camera visionCam, dugCam;
    private RenderTexture visionRT;
    private RenderTexture newDugRT, dugRT;
    private Material dugVisionMat;
    public Shader dugVisionShader;

    // Data
    /// <summary>
    /// ideal (fractional) number of pixels (of tex) high (from bottom of image) for each pixel along horizontal
    /// - cast to int for exact exclusive height
    /// </summary>
    private float[] topHeightMap, botHeightMap;
    private float[][] densityMap;
    private RockType[][] pixelRocks; // rock type for each pixel ([x][y] ground units)
    private RockType[][] collectRocks;

    // Vision System
    private List<Player> povPlayers = new List<Player>();
    private List<Unit> povUnits = new List<Unit>();
    private List<Unit> nonPovUnits = new List<Unit>();
    private bool topSurfaceVision = false;
    private bool botSurfaceVision = false;


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


    // PUBLIC MODIFIERS

    public void Init(int seed)
    {
        Make(seed);
        initialized = true;

        //StartCoroutine(UpdateVisionRoutine());
    }
    public void SetVisionPOV(Player player)
    {
        povPlayers.Add(player);

        if (player.IsTop)
            topSurfaceVision = true;
        else
            botSurfaceVision = true;

        GiveGrasslineVision(player.IsTop);
    }
    public void RegisterUnitWithVisionSys(Unit unit)
    {
        if (povPlayers.Contains(unit.Owner))
        {
            unit.SetVisible();
            povUnits.Add(unit);
            unit.visionArea.gameObject.SetActive(true);
            unit.onDestroyed += (Unit u) => { povUnits.Remove(u); };
        }
        else
        {
            nonPovUnits.Add(unit);
            unit.onDestroyed += (Unit u) => { nonPovUnits.Remove(u); };
        }
    }
    public void CollectRocks(Vector2 center, int radius, out int[] rockCounts)
    {
        rockCounts = new int[NumRockTypes];

        IVector2 c = WorldToCollectPos(center);
        int r = radius;

        for (int x = Mathf.Max(0, c.x - r); x <= Mathf.Min(c.x + r, cRocksWide-1); ++x)
        {
            for (int y = Mathf.Max(0, c.y - r); y <= Mathf.Min(c.y + r, cRocksHigh-1); ++y)
            {
                bool inCircle = MHDistance(x, y, c.x, c.y) <= r;
                if (inCircle)
                {
                    RockType rock = collectRocks[x][y];
                    rockCounts[(int)rock] += 1;
                    collectRocks[x][y] = RockType.None;
                }
            }
        }
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
        SetupRendering();
    }

    private void GenerateTerrain()
    {
        // Measurements
        pixelsWide = (int)(Resolution * Width);
        pixelsHigh = (int)(Resolution * Height);
        grassPixels = (int)(GrassHeight * Resolution);

        // Init Rock
        pixelRocks = new RockType[pixelsWide][];
        densityMap = new float[pixelsWide][];

        // Fill Rocks
        CreateHeightMaps(0.2f);
        FillSkyGrassDirt();
        //FillRocks(RockType.Rock3, 0.65f, 0.4f, 0.5f, 1f);
        //FillRocks(RockType.Rock4, 0.75f, 0.5f, 0.5f, 0.5f);
        FillRocks(RockType.Gold, 0.55f, 0.35f, 0.15f, 1f);
        FillRocks(RockType.Hardrock, 0.55f, 0.25f, 0f, 1f);

        // Create lower resolution rock grid
        MakeCollectGrid();
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
            pixelRocks[x] = new RockType[pixelsHigh];
            densityMap[x] = new float[pixelsHigh];

            int botGrassStart = (int)botHeightMap[x] + grassPixels;
            int topGrassStart = (int)topHeightMap[x] - grassPixels;

            // Sky - clear
            for (int y = 0; y <= (int)botHeightMap[x]; ++y)
                pixelRocks[x][y] = RockType.None;

            for (int y = (int)topHeightMap[x]; y < pixelsHigh; ++y)
                pixelRocks[x][y] = RockType.None;

            // Grass
            for (int y = botGrassStart; y > (int)botHeightMap[x]; --y)
                pixelRocks[x][y] = RockType.Grass;

            for (int y = topGrassStart; y < (int)topHeightMap[x]; ++y)
                pixelRocks[x][y] = RockType.Grass;

            // Dirt
            for (int y = botGrassStart + 1; y < topGrassStart; ++y)
                pixelRocks[x][y] = RockType.Dirt;
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
                    pixelRocks[x][y] = rock;
                    densityMap[x][y] = (p - perlinThreshold) / (1 - perlinThreshold);
                }

                perlin.y += perlinMove;
            }

            // Update perlin pos
            perlin.x += perlinMove;
            perlin.y = perlinStart.y;
        }
    }
    private void MakeCollectGrid()
    {
        cRocksWide = (int)(CollectResolution * Width);
        cRocksHigh = (int)(CollectResolution * Height);

        collectRocks = new RockType[cRocksWide][];

        float deltaPixels = (float)Resolution / CollectResolution;
        float halfSq = deltaPixels / 2f;
        float px = 0;
        float py = 0;

        for (int x = 0; x < cRocksWide; ++x)
        {
            collectRocks[x] = new RockType[cRocksHigh];

            for (int y = 0; y < cRocksHigh; ++y)
            {
                // Set collect rock equal to the rock of the middle-most pixel
                RockType rock = pixelRocks[(int)(px + halfSq)][(int)(py + halfSq)];
                collectRocks[x][y] = rock;
                py += deltaPixels;
            }
            px += deltaPixels;
            py = 0;
        }
    }

    private void MakeTextures()
    {
        // Ground
        tex = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(ColorsFromData());
        tex.Apply();
    }
    private Color[] ColorsFromData()
    {
        Color[] colors = new Color[pixelsWide * pixelsHigh];

        int i = 0;
        for (int y = 0; y < pixelsHigh; ++y)
        {
            for (int x = 0; x < pixelsWide; ++x)
            {
                RockType rock = pixelRocks[x][y];
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
                        colors[i] = y > pixelsHigh / 2 ? topGrassColor : botGrassColor;
                        break;
                }
                ++i;
            }
        }

        return colors;
    }
    private void SetupRendering()
    {
        dugRT = new RenderTexture(pixelsWide, pixelsHigh, 0);
        dugRT.Create();
        newDugRT = new RenderTexture(pixelsWide, pixelsHigh, 0);
        dugCam.targetTexture = dugRT;
        dugCam.orthographicSize = Height / 2f;
        dugCam.aspect = Width / Height;

        visionRT = new RenderTexture(pixelsWide, pixelsHigh, 0);
        visionCam.targetTexture = visionRT;
        visionCam.orthographicSize = Height / 2f;
        visionCam.aspect = Width / Height;

        spriteR.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), Resolution);
        spriteR.material.SetTexture("_VisionTex", visionRT);
        spriteR.material.SetTexture("_DugTex", dugRT);

        dugVisionMat = new Material(dugVisionShader);
        dugVisionMat.SetTexture("_VisionTex", visionRT);
    }

    private void Update()
    {
        if (!initialized) return;
        UpdateUnitVisibility();
        UpdateDugRTWithVision();
    }
    private void UpdateUnitVisibility()
    {
        // Update other unit visibility
        for (int i = 0; i < nonPovUnits.Count; ++i)
        {
            Unit unit = nonPovUnits[i];

            if (ClientCanSee(unit))
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
    private void UpdateDugRTWithVision()
    {
        Graphics.Blit(dugRT, newDugRT, dugVisionMat, -1);
        Graphics.CopyTexture(newDugRT, dugRT);
    }

    private void GiveGrasslineVision(bool top)
    {
        for (int x = 0; x < pixelsWide; ++x)
        {
            if (top)
            {
                Color c = topGrassColor;
                c.a = 0.5f;
                int topGrassStart = (int)topHeightMap[x] - grassPixels;
                for (int y = topGrassStart; y < (int)topHeightMap[x]; ++y)
                    tex.SetPixel(x, y, c);
            }
            else
            {
                Color c = botGrassColor;
                c.a = 0.5f;
                int botGrassStart = (int)botHeightMap[x] + grassPixels;
                for (int y = botGrassStart; y > (int)botHeightMap[x]; --y)
                    tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
    }


    // PRIVATE ACCESSORS
    
    private bool ClientCanSee(Unit unit)
    {
        // Visible if in vision range of friendly unit
        foreach (Unit povUnit in povUnits)
        {
            float dist = Vector2.Distance(unit.transform.position, povUnit.transform.position);
            if (dist < povUnit.VisionRadius)
                return true;
        }

        // Surface (grassline and above) vision
        Vector2 pixelPos = WorldToPixelPos(unit.transform.position);
        int gx = (int)pixelPos.x;
        if (gx >= 0 && gx < pixelsWide)
        {
            if (topSurfaceVision)
            {
                if (pixelPos.y > topHeightMap[gx] - grassPixels)
                    return true;
            }
            if (botSurfaceVision)
            {
                if (pixelPos.y < botHeightMap[gx] + grassPixels)
                    return true;
            }
        }
        
        return false;
    }
     
    private Vector2 WorldToPixelPos(Vector2 pos)
    {
        pos.x = ((pos.x / Width) + 0.5f) * pixelsWide;
        pos.y = ((pos.y / Height) + 0.5f) * pixelsHigh;
        return pos;
    }
    private IVector2 WorldToCollectPos(Vector2 pos)
    {
        pos.x = ((pos.x / Width) + 0.5f) * cRocksWide;
        pos.y = ((pos.y / Height) + 0.5f) * cRocksHigh;
        return new IVector2(pos);
    }
    private Vector2 CollectToWorldPos(int x, int y)
    {
        Vector2 pos = new Vector2();
        pos.x = (((float)x / cRocksWide) - 0.5f) * Width;
        pos.y = (((float)y / cRocksHigh) - 0.5f) * Height;
        return pos;
    }

    private bool InBounds(Vector2 pixelPos)
    {
        return InBounds((int)pixelPos.x, (int)pixelPos.y);
    }
    private bool InBounds(int groundX, int groundY)
    {
        return groundX >= 0 && groundX < pixelRocks.Length && groundY >= 0 && groundY < pixelRocks[0].Length;
    }
    private bool BoundsOverlap(Bounds otherBounds)
    {
        return spriteR.bounds.Intersects(otherBounds);
    }
    private Vector2 ClampPixelPosToBounds(Vector2 pixelPos)
    {
        pixelPos.x = Mathf.Min(Mathf.Max(0, pixelPos.x), pixelsWide-1);
        pixelPos.y = Mathf.Min(Mathf.Max(0, pixelPos.y), pixelsHigh - 1);
        return pixelPos;
    }
    private IVector2 ClampCRockPosToBounds(IVector2 cRockPos)
    {
        cRockPos.x = Mathf.Min(Mathf.Max(0, cRockPos.x), cRocksWide - 1);
        cRockPos.y = Mathf.Min(Mathf.Max(0, cRockPos.y), cRocksHigh - 1);
        return cRockPos;
    }


    // PRIVATE HELPERS

    private void DebugCollectRocks()
    {
        Vector2 half = Vector2.one * (1f / CollectResolution) * 0.5f;

        for (int x = 0; x < cRocksWide; ++x)
        {
            for (int y = 0; y < cRocksHigh; ++y)
            {
                Vector2 p = CollectToWorldPos(x, y);
                p += half;
                RockType rock = collectRocks[x][y];
                Tools.DebugDrawPlus(p, 
                    rock == RockType.None ? Color.red :
                    rock == RockType.Gold ? Color.yellow : Color.green,
                    0.05f);
            }
        }
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

    private static int MHDistance(int x1, int y1, int x2, int y2)
    {
        return Mathf.Abs(x2 - x1) + Mathf.Abs(y2 - y1);
    }
}