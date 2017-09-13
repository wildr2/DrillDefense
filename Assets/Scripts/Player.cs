using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class Player : NetworkBehaviour
{
    public DrillHouse drillHousePrefab;
    public Drill drillPrefab;
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
    private const int goldValue = 30; // per unit square of rock

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
            while (!gm.IsPlaying)
            {
                yield return null;
            }

            if (!isPlacing)
            {
                // Build
                if (Input.GetKeyDown(KeyCode.H))
                {
                    StartCoroutine(PlaceBuilding(drillHousePrefab));
                }
                else if (Input.GetKeyDown(KeyCode.D))
                {
                    StartCoroutine(PlaceDrill(drillPrefab));
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
            while (!gm.IsPlaying)
            {
                yield return null;
            }

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

            // Explode drill
            Drill drill = col.GetComponent<Drill>();
            if (drill != null && drill.Owner == this)
            {
                ExplodeDrill(drill);
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
        BuildingTemplate template = Instantiate(buildingPrefab.templatePrefab);
        template.Init(this, ground);
        aimLine.enabled = true;
        isPlacing = true;

        while (template != null)
        {
            // Aimline
            SetAimLine(template.transform.position,
                template.transform.position - template.transform.up * 20);

            // Confirm
            if (Input.GetMouseButtonDown(0))
            {
                if (CanBuild(drillHousePrefab))
                {
                    if (template.TargetUnit)
                    {
                        Drill drill = template.TargetUnit as Drill;
                        BuildNearDrill(drillHousePrefab, drill, template.transform.position);
                    }
                    else
                    {
                        BuildOnSurface(drillHousePrefab, template.transform.position.x);
                    }
                    break;
                }
            }

            // Cancel
            else if (Input.GetMouseButtonDown(1))
            {
                break;
            }

            yield return null;
        }

        // Cleanup
        if (template != null)
        {
            Destroy(template.gameObject);
        }
        aimLine.enabled = false;
        isPlacing = false;
    }
    private IEnumerator PlaceDrill(Drill drillPrefab)
    {
        // Create building template
        PlacementTemplate template = Instantiate(drillPrefab.templatePrefab);
        template.Init(this);
        aimLine.enabled = true;
        isPlacing = true;

        while (template != null)
        {
            // Aimline
            SetAimLine(template.transform.position,
                template.transform.position + template.transform.up * 20);

            // Confirm
            if (Input.GetMouseButtonDown(0))
            {
                DrillHouse house = template.TargetUnit as DrillHouse;
                if (house && house.CanLaunchDrill(this))
                {
                    CmdLaunchDrill(house.netId);
                    break;
                }
            }

            // Cancel
            else if (Input.GetMouseButtonDown(1))
            {
                break;
            }

            yield return null;
        }

        // Cleanup
        if (template != null)
        {
            Destroy(template.gameObject);
        }
        aimLine.enabled = false;
        isPlacing = false;
    }

    private void ExplodeDrill(Drill drill)
    {
        CmdExplodeDrill(drill.netId);
    }
    [Command]
    private void CmdExplodeDrill(NetworkInstanceId drillNetId)
    {
        Drill drill = NetworkServer.FindLocalObject(drillNetId).GetComponent<Drill>();
        if (drill != null)
            drill.Explode();
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
        ground.SetOnSurface(b.transform, xPos, IsTop);
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

}
