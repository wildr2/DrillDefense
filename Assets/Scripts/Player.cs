using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using PlaceFunc = System.Action<Placer>;

    
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
    private Placer placer;

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
    public bool IsPlacing()
    {
        return placer != null;
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

        // Aim line
        if (IsPlacing())
        {
            UpdatePlacingAimLine();
        }
    }
    private IEnumerator HumanUpdate()
    {
        while (true)
        {
            while (!gm.IsPlaying)
            {
                yield return null;
            }

            // Construction
            if (Input.GetKeyDown(KeyCode.H))
            {
                BuildingPlacer p = Instantiate(drillHousePrefab.placerPrefab);
                p.Init(this, ground);
                p.ReleaseKey = KeyCode.H;
                p.onConfirm += OnConfirmPlaceBuilding;
                StartPlacing(p);
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                DrillPlacer p = Instantiate(drillPrefab.placerPrefab);
                p.Init(this);
                p.ReleaseKey = KeyCode.D;
                p.onConfirm = OnConfirmPlaceDrill;
                StartPlacing(p);
            }

            if (!IsPlacing())
            {
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
                    BuildOnSurface(Random.Range(0, Ground.Width) - Ground.Width / 2f);
                }
            }
            else if (r < 0.4f && buildings.Count > 0)
            {
                int i = Random.Range(0, buildings.Count);
                DrillHouse b = (DrillHouse)buildings[i];
                if (CanLaunchDrillFromHouse(b))
                {
                    CmdLaunchDrillFromHouse(b.netId, b.transform.position, -b.transform.up);
                }
                    
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
            //DrillHouse house = col.GetComponent<DrillHouse>();
            //if (house != null && house.CanLaunchDrill(this))
            //{
            //    LaunchDrill(house);
            //    return;
            //}

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

    private void StartPlacing(Placer p)
    {
        if (placer != null)
        {
            Destroy(placer.gameObject);
        }

        p.onStop += OnStopPlacing;
        placer = p;
        aimLine.enabled = true;
        UpdatePlacingAimLine();
    }
    private void OnStopPlacing()
    {
        aimLine.enabled = false;
    }
    private void OnConfirmPlaceBuilding(Placer placer)
    {
        if (CanBuild(drillHousePrefab))
        {
            if (placer.TargetUnit)
            {
                Drill drill = placer.TargetUnit as Drill;
                BuildNearDrill(drill, placer.Pos, placer.Up);
                placer.Stop();
            }
            else
            {
                BuildOnSurface(placer.Pos.x);
                Destroy(placer.gameObject);
                placer.Stop();
            }
        }
    }
    private void OnConfirmPlaceDrill(Placer placer)
    {
        DrillHouse house = placer.TargetUnit as DrillHouse;
        if (house && CanLaunchDrillFromHouse(house))
        {
            CmdLaunchDrillFromHouse(house.netId, placer.Pos, placer.Up);
            placer.Stop();
            return;
        }

        Drill drill = placer.TargetUnit as Drill;
        if (drill && CanLaunchDrillFromDrill(drill))
        {
            LaunchDrillFromDrill(drill, placer.Pos, placer.Up);
            placer.Stop();
            return;
        }
    }
    
    private void UpdatePlacingAimLine()
    {
        SetAimLine(placer.Pos, placer.Pos + placer.Aim * 200);
    }
    private void SetAimLine(Vector2 p1, Vector2 p2)
    {
        aimLine.SetPosition(0, new Vector3(p1.x, p1.y, -5));
        aimLine.SetPosition(1, new Vector3(p2.x, p2.y, -5));
    }

    public bool CanLaunchDrillFromHouse(DrillHouse house)
    {
        return house.Owner == this && gold >= Drill.DrillCost;
    }
    public bool CanLaunchDrillFromDrill(Drill fromDrill)
    {
        return fromDrill.Owner == this && gold >= Drill.DrillCost;
    }
    private bool CanBuild(Building buildingPrefab)
    {
        return gold >= buildingPrefab.Cost;
    }
    private bool CanBuildNearDrill(Building buildingPrefab, Drill drill)
    {
        return drill.Owner == this && CanBuild(buildingPrefab);
    }

    private void ExplodeDrill(Drill drill)
    {
        CmdExplodeDrill(drill.netId);
    }
    private void LaunchDrillFromHouse(DrillHouse house, Vector2 pos, Vector2 up)
    {
        if (onInputLaunchDrill != null)
            onInputLaunchDrill();
        CmdLaunchDrillFromHouse(house.netId, pos, up);
    }
    private void LaunchDrillFromDrill(Drill fromDrill, Vector2 pos, Vector2 up)
    {
        if (onInputLaunchDrill != null)
            onInputLaunchDrill();
        CmdLaunchDrillFromDrill(fromDrill.netId, pos, up);
    }
    private void BuildOnSurface(float xPos)
    {
        if (onInputBuild != null)
            onInputBuild();
        CmdBuildOnSurface(xPos);
    }
    private void BuildNearDrill(Drill drill, Vector2 pos, Vector2 up)
    {
        if (onInputBuild != null)
            onInputBuild();
        CmdBuildNearDrill(drill.netId, pos, up);
    }

    [Command]
    private void CmdExplodeDrill(NetworkInstanceId drillNetId)
    {
        Drill drill = NetworkServer.FindLocalObject(drillNetId).GetComponent<Drill>();
        if (drill != null)
            drill.Explode();
    }
    [Command]
    private void CmdLaunchDrillFromHouse(NetworkInstanceId houseNetId, Vector2 pos, Vector2 up)
    {
        DrillHouse house = NetworkServer.FindLocalObject(houseNetId).GetComponent<DrillHouse>();
        if (house == null) return;

        Drill drill = Instantiate(drillPrefab);
        drill.transform.position = pos;
        drill.SetDirection(up);
        NetworkServer.Spawn(drill.gameObject);

        RpcOnLaunchDrillFromHouse(drill.netId, house.netId);
    }
    [Command]
    private void CmdLaunchDrillFromDrill(NetworkInstanceId fromDrillNetId, Vector2 pos, Vector2 up)
    {
        Drill fromDrill = NetworkServer.FindLocalObject(fromDrillNetId).GetComponent<Drill>();
        if (fromDrill == null) return;

        Drill drill = Instantiate(drillPrefab);
        drill.transform.position = pos;
        drill.SetDirection(up);
        NetworkServer.Spawn(drill.gameObject);

        RpcOnLaunchDrillFromDrill(drill.netId, fromDrill.netId);
    }
    [Command]
    private void CmdBuildOnSurface(float xPos)
    {
        Building b = Instantiate(drillHousePrefab);
        ground.SetOnSurface(b.transform, xPos, IsTop);
        NetworkServer.Spawn(b.gameObject);
        RpcOnBuild(b.netId);
    }
    [Command]
    private void CmdBuildNearDrill(NetworkInstanceId drillNetId, Vector2 pos, Vector2 up)
    {
        Building b = Instantiate(drillHousePrefab);
        b.transform.position = pos;
        b.transform.up = up;
        NetworkServer.Spawn(b.gameObject);

        RpcOnBuild(b.netId);
    }

    [ClientRpc]
    private void RpcOnLaunchDrillFromHouse(NetworkInstanceId drillNetId, NetworkInstanceId houseNetId)
    {
        Drill drill = ClientScene.FindLocalObject(drillNetId).GetComponent<Drill>();
        DrillHouse house = ClientScene.FindLocalObject(houseNetId).GetComponent<DrillHouse>();

        drill.Init(this);
        gold -= Drill.DrillCost;
        drill.onDig += OnDrillDig;

        Physics2D.IgnoreCollision(house.GetComponent<Collider2D>(),
            drill.GetComponent<Collider2D>());
    }
    [ClientRpc]
    private void RpcOnLaunchDrillFromDrill(NetworkInstanceId drillNetId, NetworkInstanceId fromDrillNetId)
    {
        Drill drill = ClientScene.FindLocalObject(drillNetId).GetComponent<Drill>();
        Drill fromDrill = ClientScene.FindLocalObject(fromDrillNetId).GetComponent<Drill>();

        drill.Init(this);
        gold -= Drill.DrillCost;
        drill.onDig += OnDrillDig;

        Physics2D.IgnoreCollision(drill.GetComponent<Collider2D>(),
            fromDrill.GetComponent<Collider2D>());
    }
    [ClientRpc]
    private void RpcOnBuild(NetworkInstanceId buildingNetId)
    {
        Building building = ClientScene.FindLocalObject(buildingNetId).GetComponent<Building>();

        building.Init(this);
        buildings.Add(building);
        building.onDestroyed += (Unit u) => OnBuildingDestroyed(u.GetComponent<Building>());
        gold -= building.Cost;
    }
}
