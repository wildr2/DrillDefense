using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int id;
    private float gold = 100;

    public DrillHouse drillHousePrefab;


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

        while (true)
        {
            Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            template.position = mouse;

            if (Input.GetMouseButtonDown(0)) // left click
            {
                if (gold > buildingPrefab.Cost)
                {
                    // Placed successfully
                    Building b = Instantiate(buildingPrefab);
                    b.transform.position = template.transform.position;
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
