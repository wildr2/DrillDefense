using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drill : MonoBehaviour
{
    private float speed = 1; // units per second
    private Ground ground;
    public SpriteRenderer colliderSprite;

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

        //if (ground.IsGround(transform.position))
        //{
        //    ground.DrillLine(oldPos, transform.position, 1);
        //    Debug.DrawLine(transform.position, (Vector2)transform.position + heading, Color.red);
        //}

        if (ground.SpriteOverlaps(colliderSprite))
        {
            Debug.DrawLine(transform.position, (Vector2)transform.position + heading, Color.red);
        }

    }
}
