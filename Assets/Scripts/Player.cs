using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;


public class Player : NetworkBehaviour
{
    [SyncVar] public int id;
    public bool ai = false;

    public bool IsTop { get; private set; }
    private float gold = 100;
    private List<Building> buildings = new List<Building>();
    private LineRenderer aimLine;

    //public Text uiGold;
    public DrillHouse drillHousePrefab;
    private Ground ground;


    private void Awake()
    {
        ground = FindObjectOfType<Ground>();
        aimLine = GetComponent<LineRenderer>();

        aimLine.enabled = false;
    }
    private void Start()
    {
        IsTop = id == 0;

        GameManager gm = FindObjectOfType<GameManager>();
        gm.RegisterPlayer(this);

        // Start update routine
        if (ai)
        {
            // note: should always be the server (1v1)
            StartCoroutine(AIUpdate());
        }
        else if (isLocalPlayer)
        {
            StartCoroutine(HumanUpdate());
        }
    }
    private void Update()
    {
        // Gold
        //gold += Time.deltaTime * 3;
        //uiGold.text = Mathf.FloorToInt(gold).ToString();
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
                    if (house != null && house.CanLaunchDrill((int)gold))
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
            //float r = Random.value;
            //if (r < 0.3f)
            //{
            //    if (CanBuild(drillHousePrefab))
            //    {
            //        Build(drillHousePrefab, Random.Range(0, ground.Width) - ground.Width / 2f);
            //    }
            //}
            //else if (r < 0.4f && buildings.Count > 0)
            //{
            //    int i = Random.Range(0, buildings.Count);
            //    DrillHouse b = (DrillHouse)buildings[i];
            //    TryLaunchDrill(b);
            //}
            
               
            //yield return new WaitForSeconds(1);
        }
    }

    private void OnBuildingDestroyed(Building b)
    {
        buildings.Remove(b);
    }
    private void OnDrillDig(Dictionary<RockType, int> digCount)
    {
        gold += digCount[RockType.Gold] / 10f;
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

        if (house.CanLaunchDrill((int)gold))
        {
            Drill drill = house.LaunchDrill();
            drill.onDig += OnDrillDig;
            gold -= DrillHouse.drillCost;
        }
    }

    private bool CanBuild(Building buildingPrefab)
    {
        return gold >= buildingPrefab.Cost;
    }
    private void Build(Building buildingPrefab, float xPos)
    {
        Building b = Instantiate(buildingPrefab);
        SetOnSurface(b.transform, xPos);
        NetworkServer.Spawn(b.gameObject);

        //buildings.Add(b);
        //b.onDestroyed += OnBuildingDestroyed;

        gold -= buildingPrefab.Cost;
    }
}
