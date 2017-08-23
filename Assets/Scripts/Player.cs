using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class Player : NetworkBehaviour
{
    [SyncVar] public int id;
    public bool ai = false;

    public bool IsTop { get; private set; }
    public float Gold { get; private set; }
    private List<Building> buildings = new List<Building>();
    private LineRenderer aimLine;

    public DrillHouse drillHousePrefab;
    private Ground ground;
    private GameManager gm;


    // PRIVATE MODIFIERS

    private void Awake()
    {
        gm = FindObjectOfType<GameManager>();
        ground = FindObjectOfType<Ground>();
        aimLine = GetComponent<LineRenderer>();
        
        aimLine.enabled = false;
        Gold = 120;
    }
    private void Start()
    {
        IsTop = id == 0;
        gm.RegisterPlayer(this);
        if (isLocalPlayer)
        {
            if (gm.PlayersReady) StartUpdateLoop();
            else gm.onPlayersReady += StartUpdateLoop;
        }
    }
    private void StartUpdateLoop()
    {
        if (ai)
        {
            // note: should always be the server (1v1)
            StartCoroutine(AIUpdate());
        }
        else
        {
            StartCoroutine(HumanUpdate());
        }
    }
    private void Update()
    {
        // Gold
        //Gold += Time.deltaTime * 3;
    }
    private IEnumerator HumanUpdate()
    {
        while (true)
        {
            // Build
            if (Input.GetKeyDown(KeyCode.H))
            {
                StartCoroutine(PlaceBuilding(drillHousePrefab));
            }

            // Click on building
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Collider2D col = Physics2D.OverlapPoint(mouse);
                if (col != null)
                {
                    DrillHouse house = col.GetComponent<DrillHouse>();
                    if (house != null && house.CanLaunchDrill(this, (int)Gold))
                        CmdLaunchDrill(house.netId);
                }
            }
            yield return null;
        }
    }
    private IEnumerator AIUpdate()
    {
        while (true)
        {
            float r = Random.value;
            if (r < 0.3f)
            {
                if (CanBuild(drillHousePrefab))
                {
                    CmdBuild(Random.Range(0, ground.Width) - ground.Width / 2f);
                }
            }
            else if (r < 0.4f && buildings.Count > 0)
            {
                int i = Random.Range(0, buildings.Count);
                DrillHouse b = (DrillHouse)buildings[i];
                if (b.CanLaunchDrill(this, (int)Gold))
                    CmdLaunchDrill(b.netId);
            }

            yield return new WaitForSeconds(1);
        }
    }

    private void OnBuildingDestroyed(Building b)
    {
        buildings.Remove(b);
    }
    private void OnDrillDig(Dictionary<RockType, int> digCount)
    {
        Gold += digCount[RockType.Gold] / 10f;
    }

    private IEnumerator PlaceBuilding(Building buildingPrefab)
    {
        // Create building template
        Transform template = Instantiate(buildingPrefab.templatePrefab);
        aimLine.enabled = true;

        while (true)
        {
            // Set template position / orientation
            Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            SetOnSurface(template, mouse.x);
            aimLine.SetPosition(0, template.position);
            aimLine.SetPosition(1, template.position + -template.up * 20);

            // Actions
            if (Input.GetMouseButtonDown(0)) // Build on left click
            {
                if (CanBuild(drillHousePrefab))
                {
                    CmdBuild(template.position.x);
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
    }
    private void SetOnSurface(Transform thing, float xPos)
    {
        thing.up = ground.GetNormalAt(xPos, IsTop);
        thing.position = new Vector2(xPos, ground.GetHeightAt(xPos, IsTop));
        thing.position -= thing.up * 0.1f;
    }

    [Command]
    private void CmdBuild(float xPos)
    {
        if (CanBuild(drillHousePrefab))
        {
            Build(drillHousePrefab, xPos);
        }
    }
    [Command]
    private void CmdLaunchDrill(NetworkInstanceId houseNetId)
    {
        DrillHouse house = NetworkServer.FindLocalObject(houseNetId).GetComponent<DrillHouse>();
        if (house == null) return;

        if (house.CanLaunchDrill(this, (int)Gold))
        {
            Drill drill = house.LaunchDrill();
            drill.onDig += OnDrillDig;
            Gold -= DrillHouse.drillCost;
        }
    }

    private bool CanBuild(Building buildingPrefab)
    {
        return Gold >= buildingPrefab.Cost;
    }
    private void Build(Building buildingPrefab, float xPos)
    {
        Building b = Instantiate(buildingPrefab);
        SetOnSurface(b.transform, xPos);
        NetworkServer.Spawn(b.gameObject);

        if (!isClient) OnBuild(b);
        RpcOnBuild(b.netId);
    }
    private void OnBuild(Building building)
    {
        building.Init(this);
        buildings.Add(building);
        building.onDestroyed += OnBuildingDestroyed;
        Gold -= building.Cost;
    }
    [ClientRpc]
    private void RpcOnBuild(NetworkInstanceId buildingNetId)
    {
        OnBuild(ClientScene.FindLocalObject(buildingNetId).GetComponent<Building>());
    }


}
