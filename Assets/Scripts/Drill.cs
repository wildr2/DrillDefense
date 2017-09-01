using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Drill : Unit
{
    public override float VisionRadius { get { return 2; } }
    private float health = 60;
    private float speed = 1; // units per second

    public SpriteRenderer colliderSprite;
    new private PolygonCollider2D collider;
    private Ground ground;

    // args: rockCounts 
    public System.Action<int[]> onDig;
 

    public override void Init(Player owner)
    {
        base.Init(owner);
    }
    public void SetDirection(Vector2 dir)
    {
        //fallGravity = -Vector2.Dot(dir, Vector2.up);
        transform.up = -dir;
    }


    protected override void Awake()
    {
        base.Awake();
        ground = FindObjectOfType<Ground>();
        collider = GetComponent<PolygonCollider2D>();
    }
    private void Update()
    {
        // Dig
        int[] rockCounts;
        if (ground.DigPolygon(collider, out rockCounts) > 0)
        {
            // Decrease Health
            health -= rockCounts[(int)RockType.Gold] / 10f;
            health -= rockCounts[(int)RockType.Hardrock] / 2f;

            if (onDig != null) onDig(rockCounts);
        }

        // Move
        transform.position -= transform.up * speed * Time.deltaTime;

        // Death
        if (health <= 0 || IsOutOfBounds())
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

    public bool IsOutOfBounds()
    {
        return Mathf.Abs(transform.position.y) > ground.Height * 0.7f;
    }
}
