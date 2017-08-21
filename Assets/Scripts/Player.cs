using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public int id;
    public bool isTop = true;
    public bool ai = false;
    private float gold = 60;

    private LineRenderer aimLine;

    public Text uiGold;
    public DrillHouse drillHousePrefab;
    private Ground ground;

    private List<Building> buildings = new List<Building>();


    private void Awake()
    {
        ground = FindObjectOfType<Ground>();
        aimLine = GetComponent<LineRenderer>();

        aimLine.enabled = false;
    }
    private void Start()
    {
        if (ai) StartCoroutine(AIUpdate());
        else StartCoroutine(HumanUpdate());
    }
    private void Update()
    {
        // Gold
        //gold += Time.deltaTime * 3;
        uiGold.text = Mathf.FloorToInt(gold).ToString();
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
                    if (house != null) TryLaunchDrill(house);
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
                    Build(drillHousePrefab, Random.Range(0, ground.Width) - ground.Width / 2f);
                }
            }
            else if (r < 0.4f && buildings.Count > 0)
            {
                int i = Random.Range(0, buildings.Count);
                DrillHouse b = (DrillHouse)buildings[i];
                TryLaunchDrill(b);
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
        gold += digCount[RockType.Gold] / 10f;
    }

    private bool TryLaunchDrill(DrillHouse house)
    {
        if (house.CanLaunchDrill((int)gold))
        {
            Drill drill = house.LaunchDrill();
            drill.onDig += OnDrillDig;
            gold -= DrillHouse.drillCost;
            return true;
        }
        return false;
    }
    private void SetOnSurface(Transform thing, float xPos)
    {
        thing.up = ground.GetNormalAt(xPos, isTop);
        thing.position = new Vector2(xPos, ground.GetHeightAt(xPos, isTop));
        thing.position -= thing.up * 0.1f;
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
                if (TryBuild(buildingPrefab, template.position.x)) break;
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
    private bool TryBuild(Building buildingPrefab, float xPos)
    {
        if (CanBuild(buildingPrefab))
        {
            Build(buildingPrefab, xPos);
            return true;
        }
        return false;
    }
    private bool CanBuild(Building buildingPrefab)
    {
        return gold >= buildingPrefab.Cost;
    }
    private void Build(Building buildingPrefab, float xPos)
    {
        Building b = Instantiate(buildingPrefab);
        SetOnSurface(b.transform, xPos);
        buildings.Add(b);
        b.onDestroyed += OnBuildingDestroyed;

        gold -= buildingPrefab.Cost;
    }
}
