using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Drill : NetworkBehaviour
{
    public Player Owner { get; private set; }

    private float health = 60;
    private float speed = 1; // units per second
    public SpriteRenderer colliderSprite;

    private Ground ground;

    public System.Action<Dictionary<RockType, int>> onDig;



    public virtual void Init(Player owner)
    {
        Owner = owner;
    }
    public void SetDirection(Vector2 dir)
    {
        //fallGravity = -Vector2.Dot(dir, Vector2.up);
        transform.up = -dir;
    }


    private void Awake()
    {
        ground = FindObjectOfType<Ground>();
        //rb = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {
        // Dig
        Dictionary<RockType, int> digCount;
        bool dug = ground.DigWithSprite(colliderSprite, out digCount);

        if (dug)
        {
            // Decrease Health
            health -= digCount[RockType.Gold] / 10f;
            health -= digCount[RockType.Hardrock] / 2f;

            if (onDig != null) onDig(digCount);
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
        return transform.position.y > ground.GetSkyY(Owner.Up);
    }
}
