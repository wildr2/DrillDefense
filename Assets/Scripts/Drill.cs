using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Drill : Unit
{
    public override float KillGold { get { return 0; } }

    private float health = 1;
    private float speed = 1; // units per second

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
        Vector3 center = transform.position + transform.up * 0.5f;
        ground.CollectRocks(center, 1, out rockCounts);

        // Decrease Health
        health -= rockCounts[(int)RockType.Gold] * Ground.RockValue * 0.5f;
        health -= rockCounts[(int)RockType.Hardrock] * Ground.RockValue * 3f;

        if (onDig != null) onDig(rockCounts);
        

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
            RpcOnCollideDrill(drill.netId);
        }
    }

    [ClientRpc]
    private void RpcOnCollideDrill(NetworkInstanceId drillNetId)
    {
        OnCollideDrill(ClientScene.FindLocalObject(drillNetId).GetComponent<Drill>());
    }
    private void OnCollideDrill(Drill drill)
    {
        Kill(drill.Owner);
    }

    public bool IsOutOfBounds()
    {
        return Mathf.Abs(transform.position.y) > Ground.Height * 0.7f;
    }
}
