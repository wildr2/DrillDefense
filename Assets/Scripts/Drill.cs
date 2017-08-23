using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Drill : NetworkBehaviour
{
    private float speed = 1; // units per second
    public float fallGravity = 1;
    private float fallTime = 0;
    private const float maxFallTime = 5;
    private float health = 60;

    private Rigidbody2D rb;
    public SpriteRenderer colliderSprite;

    private Ground ground;

    public System.Action<Dictionary<RockType, int>> onDig;


    public void SetDirection(Vector2 dir)
    {
        fallGravity = -Vector2.Dot(dir, Vector2.up);
        transform.up = -dir;
    }

    private void Awake()
    {
        ground = FindObjectOfType<Ground>();
        rb = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {
        // Dig
        Dictionary<RockType, int> digCount;
        bool dug = ground.DigWithSprite(colliderSprite, out digCount);

        if (dug)
        {
            if (rb.gravityScale != 0)
            {
                // Stop falling
                rb.gravityScale = 0;
                fallTime = 0;
            }

            // Move Forwards
            Vector2 heading = -transform.up;
            rb.velocity = heading * speed;

            // Decrease Health
            health -= digCount[RockType.Gold] / 10f;
            health -= digCount[RockType.Hardrock] / 2f;

            if (onDig != null) onDig(digCount);
        }
        else
        {
            // Fall
            rb.gravityScale = fallGravity * 0.25f;
            fallTime += Time.deltaTime;
            if (fallTime > maxFallTime)
            {
                Destroy(gameObject);
            }
        }

        // Death
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isServer) return;

        Drill drill = collision.collider.GetComponent<Drill>();
        if (drill != null)
        {
            if (!isClient) OnCollideDrill();
            RpcOnCollideDrill();
        }
    }

    [ClientRpc]
    private void RpcOnCollideDrill()
    {
        OnCollideDrill();
    }
    private void OnCollideDrill()
    {
        Destroy(gameObject);
    }
}
