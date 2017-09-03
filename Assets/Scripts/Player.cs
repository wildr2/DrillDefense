using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class Player : NetworkBehaviour
{
    public DrillHouse drillHousePrefab;
    private Ground ground;
    private GameManager gm;

    private LineRenderer aimLine;

    [SyncVar] public short id;
    [SyncVar] public float gold;

    // General
    public Color Color { get; private set; }
    public bool ai = false;
    public bool IsTop { get; private set; }
    public Vector2 Up { get { return IsTop ? Vector2.up : -Vector2.up; } }

    // Gold
    private const int startGold = 100;
    private const int goldPerSecond = 1;
    private const int goldValue = 20; // per unit square of rock

    // Construction
    private List<Building> buildings = new List<Building>();
    private bool isPlacing = false;

    // Events
    public System.Action onInputBuild;
    public System.Action onInputLaunchDrill;



    // PUBLIC ACCESSORS

    public int GetGold()
    {
        return Mathf.FloorToInt(gold);
    }
    public bool IsLocalHuman()
    {
        return isLocalPlayer && !ai;
    }


    // PRIVATE MODIFIERS

    private void Awake()
    {
        gm = FindObjectOfType<GameManager>();
        ground = FindObjectOfType<Ground>();
        aimLine = GetComponent<LineRenderer>();
        
        aimLine.enabled = false;
    }
    private void Start()
    {
        DataManager dm = DataManager.Instance;

        IsTop = id == 0;
        Color = dm.playerColors[id];

        gold = startGold;
        if (dm.debug_powers)
        {
            gold = 10000;
        }

        gm.RegisterPlayer(this);
        StartUpdateLoop();
    }
    private void StartUpdateLoop()
    {
        if (isLocalPlayer)
        {
            if (ai)
            {
                if (!DataManager.Instance.aiDoNothing)
                    StartCoroutine(AIUpdate());
            }
            else
            {
                StartCoroutine(HumanUpdate());
            }
        }
    }
    private void Update()
    {
        if (!gm.IsPlaying) return;
        gold += goldPerSecond * Time.deltaTime;
    }
    private IEnumerator HumanUpdate()
    {
        while (true)
        {
            if (!gm.IsPlaying) yield return null;

            if (!isPlacing)
            {
                // Build
                if (Input.GetKeyDown(KeyCode.H))
                {
                    StartCoroutine(PlaceBuilding(drillHousePrefab));
                }

                // Click action
                UpdateClickAction();
            }

            yield return null;
        }
    }
    private IEnumerator AIUpdate()
    {
        while (true)
        {
            if (!gm.IsPlaying) yield return null;

            float r = Random.value;
            if (r < 0.3f)
            {
                if (CanBuild(drillHousePrefab))
                {
                    BuildOnSurface(drillHousePrefab, Random.Range(0, Ground.Width) - Ground.Width / 2f);
                }
            }
            else if (r < 0.4f && buildings.Count > 0)
            {
                int i = Random.Range(0, buildings.Count);
                DrillHouse b = (DrillHouse)buildings[i];
                if (b.CanLaunchDrill(this))
                    CmdLaunchDrill(b.netId);
            }

            yield return new WaitForSeconds(1);
        }
    }
    private void UpdateClickAction()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = Physics2D.OverlapPoint(mouse);
            if (col == null) return;

            // Launch Drill
            DrillHouse house = col.GetComponent<DrillHouse>();
            if (house != null && house.CanLaunchDrill(this))
            {
                LaunchDrill(house);
                return;
            }
        }
    }

    private void OnBuildingDestroyed(Building b)
    {
        buildings.Remove(b);
    }
    private void OnDrillDig(int[] rockCounts)
    {
        gold += rockCounts[(int)RockType.Gold] * Ground.RockValue * goldValue;
    }

    private void SetAimLine(Vector2 p1, Vector2 p2)
    {
        aimLine.SetPosition(0, new Vector3(p1.x, p1.y, -5));
        aimLine.SetPosition(1, new Vector3(p2.x, p2.y, -5));
    }

    private IEnumerator PlaceBuilding(Building buildingPrefab)
    {
        // Create building template
        Transform template = Instantiate(buildingPrefab.templatePrefab);
        aimLine.enabled = true;
        isPlacing = true;

        Drill nearbyDrill = null;
        bool drillLock = false;

        while (true)
        {
            Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
 
            if (drillLock)
            {
                if (nearbyDrill == null)
                {
                    // Locked drill destroyed - cancel
                    break;
                }

                // Set template pos / ori near locked drill
                Vector2 drillPos = nearbyDrill.transform.position;
                template.up = drillPos - mouse;
                template.position = drillPos + (Vector2)template.up * 1.25f;
            }
            else
            {
                // Lock to nearby drill
                nearbyDrill = GetNearestDrill(mouse, 1, true);
                if (nearbyDrill != null)
                    drillLock = true;

                // Set template pos / ori on surface
                SetOnSurface(template, mouse.x);
            }

            // Aimline
            SetAimLine(template.position, template.position + -template.up * 20);

            // Actions
            if (Input.GetMouseButtonDown(0)) // Build on left click
            {
                if (CanBuild(drillHousePrefab))
                {
                    if (nearbyDrill != null)
                    {
                        BuildNearDrill(drillHousePrefab, nearbyDrill, template.position);
                    }
                    else
                    {
                        BuildOnSurface(drillHousePrefab, template.position.x);
                    }
                    break;
                }
            }
            else if (Input.GetMouseButtonDown(1)) // Cancel on right click
            {
                break;
            }

            yield return null;
        }

        // Cleanup
        Destroy(template.gameObject);
        aimLine.enabled = false;
        isPlacing = false;
    }
    private void SetOnSurface(Transform thing, float xPos)
    {
        thing.up = ground.GetNormalAt(xPos, IsTop);
        thing.position = new Vector2(xPos, ground.GetHeightAt(xPos, IsTop));
        thing.position -= thing.up * 0.1f;
    }


    private void LaunchDrill(DrillHouse house)
    {
        if (onInputLaunchDrill != null)
            onInputLaunchDrill();
        CmdLaunchDrill(house.netId);
    }
    [Command]
    private void CmdLaunchDrill(NetworkInstanceId houseNetId)
    {
        DrillHouse house = NetworkServer.FindLocalObject(houseNetId).GetComponent<DrillHouse>();
        if (house == null) return;

        Drill drill = house.LaunchDrill();
        RpcOnLaunchDrill(drill.netId);
    }
    private void OnLaunchDrill(Drill drill)
    {
        gold -= DrillHouse.DrillCost;
        drill.onDig += OnDrillDig;
    }
    [ClientRpc]
    private void RpcOnLaunchDrill(NetworkInstanceId drillNetId)
    {
        OnLaunchDrill(ClientScene.FindLocalObject(drillNetId).GetComponent<Drill>());
    }


    private bool CanBuildNearDrill(Building buildingPrefab, Drill drill)
    {
        return drill.Owner == this && CanBuild(buildingPrefab);
    }
    private void BuildNearDrill(Building buildingPrefab, Drill drill, Vector2 pos)
    {
        if (onInputBuild != null)
            onInputBuild();
        CmdBuildNearDrill(pos, drill.transform.position);
    }
    [Command]
    private void CmdBuildNearDrill(Vector2 pos, Vector2 drillPos)
    {
        Building b = Instantiate(drillHousePrefab);
        b.transform.position = pos;
        b.transform.up = pos - drillPos;
        NetworkServer.Spawn(b.gameObject);

        RpcOnBuild(b.netId);
    }

    private bool CanBuild(Building buildingPrefab)
    {
        return gold >= buildingPrefab.Cost;
    }
    private void BuildOnSurface(Building buildingPrefab, float xPos)
    {
        if (onInputBuild != null)
            onInputBuild();
        CmdBuildOnSurface(xPos);
    }
    [Command]
    private void CmdBuildOnSurface(float xPos)
    {
        Building b = Instantiate(drillHousePrefab);
        SetOnSurface(b.transform, xPos);
        NetworkServer.Spawn(b.gameObject);
        RpcOnBuild(b.netId);
    }

    private void OnBuild(Building building)
    {
        building.Init(this);
        buildings.Add(building);
        building.onDestroyed += (Unit u) => OnBuildingDestroyed(u.GetComponent<Building>());
        gold -= building.Cost;
    }
    [ClientRpc]
    private void RpcOnBuild(NetworkInstanceId buildingNetId)
    {
        OnBuild(ClientScene.FindLocalObject(buildingNetId).GetComponent<Building>());
    }



    // PRIVATE HELPERS

    private Drill GetNearestDrill(Vector2 point, float range, bool friendlyOnly=false)
    {
        Drill nearest = null;
        float minDist = range + 1;

        Collider2D[] cols = Physics2D.OverlapCircleAll(point, range);
        foreach (Collider2D col in cols)
        {
            Drill drill = col.GetComponent<Drill>();
            if (drill == null) continue;
            if (friendlyOnly && drill.Owner != this) continue;

            float dist = Vector2.Distance(point, drill.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = drill;
            }
        }
        return nearest;
    }
}
