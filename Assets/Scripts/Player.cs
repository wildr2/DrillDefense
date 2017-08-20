using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int id;
    private float gold = 100;

    public DrillHouse drillHousePrefab;
    private Ground ground;

    private void Awake()
    {
        ground = FindObjectOfType<Ground>();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            StartCoroutine(PlaceBuilding(drillHousePrefab));
        }
    }

    private IEnumerator PlaceBuilding(Building buildingPrefab)
    {
        Transform template = Instantiate(buildingPrefab.templatePrefab);
        float templateHeight = template.GetComponent<SpriteRenderer>().bounds.extents.y;

        while (true)
        {
            // Set template position
            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            pos.y = ground.GetHeightAt(pos.x, true) + templateHeight / 2f;
            template.position = pos;

            // Set template orientation
            template.transform.up = ground.GetNormalAt(pos.x, true);

            if (Input.GetMouseButtonDown(0)) // left click
            {
                if (gold > buildingPrefab.Cost)
                {
                    // Placed successfully
                    Building b = Instantiate(buildingPrefab);
                    b.transform.position = template.transform.position;
                    b.transform.rotation = template.transform.rotation;
                    break;
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                // Cancel
                break;
            }

            yield return null;
        }

        // Cleanup
        Destroy(template.gameObject);
    }
}
