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
        GenData dat = GenerateTerrain();
        MakeTextures(dat.pixels);
        SetupRendering();
    }

    private GenData GenerateTerrain()
    {
        // Measurements
        pixelsWide = (int)(Resolution * Width);
        pixelsHigh = (int)(Resolution * Height);
        grassPixels = (int)(GrassHeight * Resolution);

        // Data
        GenData dat = new GenData(pixelsWide, pixelsHigh);

        // Create
        CreateHeightMaps(0.2f);
        Fill(dat);

        // Create lower resolution rock grid
        MakeCollectGrid(dat);

        return dat;
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
    private void Fill(GenData dat)
    {
        int x = 0;
        int y = 0;

        for (int i = 0; i < dat.pixels.Length; ++i)
        {
            // Fill
            dat.density = 0;
            if  (FillSky(dat, x, y, i)) { }
            else if (FillTopGrass(dat, x, y, i)) { }
            else if (FillBotGrass(dat, x, y, i)) { }
            else if (FillHardrock(dat, x, y, i)) { }
            else if (FillGold(dat, x, y, i)) { }
            else if (FillPearl(dat, x, y, i)) { }
            else FillDirt(dat, x, y, i);

            // Increment pixel coords
            ++x;
            if (x == pixelsWide)
            {
                x = 0;
                y += 1;
                dat.SetDepth(y);
            }
        }
    }
    private bool FillSky(GenData dat, int x, int y, int i)
    {
        if (y >= (int)topHeightMap[x] || y <= (int)botHeightMap[x])
        {
            // Fill
            dat.pixels[i] = clearColor;
            dat.rocks[x][y] = RockType.None;
            return true;
        }
        return false;
    }
    private bool FillTopGrass(GenData dat, int x, int y, int i)
    {
        if (y >= (int)topHeightMap[x] - grassPixels)
        {
            // Fill
            dat.pixels[i] = topGrassColor;
            dat.rocks[x][y] = RockType.Grass;
            return true;
        }
        return false;
    }
    private bool FillBotGrass(GenData dat, int x, int y, int i)
    {
        if (y <= (int)botHeightMap[x] + grassPixels)
        {
            // Fill
            dat.pixels[i] = botGrassColor;
            dat.rocks[x][y] = RockType.Grass;
            return true;
        }
        return false;
    }
    private bool FillHardrock(GenData dat, int x, int y, int i)
    {
        float p = dat.Perlin(x, y, 0.0055f, 0);
        dat.SetDensity(p, 0.57f);

        if (dat.density >= 1)
        {
            // Fill
            dat.pixels[i] = hardrockColor;
            dat.rocks[x][y] = RockType.Hardrock;
            return true;
        }
        return false;
    }
    private bool FillGold(GenData dat, int x, int y, int i)
    {
        float p = dat.Perlin(x, y, 0.0065f, 1);
        dat.SetDensity(p, 0.53f, dat.GetDepthFactor(1, 0.25f));

        if (dat.density >= 1)
        {
            // Fill
            float t = (dat.density - 1) / (1 - 0.53f);
            t = (int)(t * 2) / 2f;
            dat.pixels[i] = Color.Lerp(dirtColor, goldColor, 0.8f + t);
            //dat.pixels[i] = goldColor;
            dat.rocks[x][y] = RockType.Gold;
            return true;
        }
        return false;
    }
    private bool FillPearl(GenData dat, int x, int y, int i)
    {
        float p = dat.Perlin(x, y, 0.01f, 2);
        dat.SetDensity(p, 0.63f, dat.GetDepthFactor(0.45f, 1));

        if (dat.density >= 1)
        {
            // Fill
            //float t = (dat.density - 1) / (1 - 0.53f);
            //t = (int)(t * 2) / 2f;
            //dat.pixels[i] = Color.Lerp(dirtColor, goldColor, 0.8f + t);
            dat.pixels[i] = rock4Color;
            dat.rocks[x][y] = RockType.Rock4;
            return true;
        }
        return false;
    }
    private bool FillDirt(GenData dat, int x, int y, int i)
    {
        // Fill
        dat.pixels[i] = dirtColor;
        dat.rocks[x][y] = RockType.Dirt;
        return true;
    }
    private void MakeCollectGrid(GenData dat)
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
                RockType rock = dat.rocks[(int)(px + halfSq)][(int)(py + halfSq)];
                collectRocks[x][y] = rock;
                py += deltaPixels;
            }
            px += deltaPixels;
            py = 0;
        }
    }

    private void MakeTextures(Color[] pixels)
    {
        // Ground
        tex = new Texture2D(pixelsWide, pixelsHigh, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(pixels);
        tex.Apply();
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
        //DebugCollectRocks();
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


    class GenData
    {
        // Generation output
        public Color[] pixels;
        public RockType[][] rocks;

        // Noise
        private Vector2 perlinStart;
        private float perlinOffsetScale;

        // General
        private float height;
        private int midY;

        // Current pixel info
        public float density;
        private float depth = 0;


        public GenData(int pixelsWide, int pixelsHigh)
        {
            height = pixelsHigh;
            midY = pixelsHigh / 2;

            perlinOffsetScale = pixelsWide;
            perlinStart = new Vector2(Random.value, Random.value) * perlinOffsetScale;

            pixels = new Color[pixelsWide * pixelsHigh];
            rocks = new RockType[pixelsWide][];

            for (int x = 0; x < pixelsWide; ++x)
            {
                rocks[x] = new RockType[pixelsHigh];
            }
        }
        public void SetDepth(int y)
        {
            depth = 1 - (Mathf.Abs(y - midY) / height) * 2f;
        }
        public void SetDensity(float noise, float rockThreshold, float depthFactor=1)
        {
            float d = (noise / rockThreshold - Mathf.Pow(density, 16)) * depthFactor;
            density = Mathf.Max(density, d);
        }
        public float Perlin(int x, int y, float delta, int offsets)
        {
            return Mathf.PerlinNoise(
                perlinStart.x + offsets * perlinOffsetScale + x * delta,
                perlinStart.y + y * delta);
        }
        public float GetDepthFactor(float idealDepth=1, float depthImportance=0.5f)
        {
            float dist = 1 - Mathf.Abs(idealDepth - depth);
            return Mathf.Pow(dist, depthImportance);
        }
    }
}
