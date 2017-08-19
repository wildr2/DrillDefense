using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drill : MonoBehaviour
{
    private float speed = 1; // units per second
    private Ground ground;

    private void Awake()
    {
        ground = FindObjectOfType<Ground>();
    }
    private void Update()
    {
        Vector2 oldPos = transform.position;
        Vector2 heading = -transform.up;
        Vector2 velocity = heading * speed;
        transform.Translate(velocity * Time.deltaTime, Space.World);

        ground.DrillLine(oldPos, transform.position, 1);
    }
}
