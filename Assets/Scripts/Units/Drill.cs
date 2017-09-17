using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Drill : Unit
{
    public override float KillGold { get { return 0; } }
    public const int DrillCost = 20;

    private Vector2 position0;
    private float lifeTime = 0;
    private float health = 1;
    private float speed = 1; // units per second
    private float timeSinceDigDamage = 100;
    private bool exploding = false;
    private const float shakeTimeWindow = 0.5f;

    private SpriteRenderer spriteR;

    public DrillPlacer placerPrefab;
    public Transform diggerCircle;
    public Transform shakingParent;

    private Ground ground;

    // args: rockCounts 
    public System.Action<int[]> onDig;


    public override void Init(Player owner)
    {
        position0 = transform.position;
        base.Init(owner);
    }
    public void SetDirection(Vector2 dir)
    {
        transform.up = dir;
    }
    public void Explode()
    {
        RpcOnExplode();
    }


    protected override void Awake()
    {
        base.Awake();
        ground = FindObjectOfType<Ground>();
        spriteR = graphics.GetComponentInChildren<SpriteRenderer>();
    }
    private void Update()
    {
        // Dig
        int[] rockCounts;
        Vector3 center = transform.position - transform.up * 0.5f;
        ground.CollectRocks(center, exploding ? 4 : 1, out rockCounts);

        // Dig Damage
        float damage = rockCounts[(int)RockType.Gold] * Ground.RockValue * 0.7f;
        damage += rockCounts[(int)RockType.Hardrock] * Ground.RockValue * 3f;
        if (damage > 0)
        {
            timeSinceDigDamage = 0;
            health -= damage;
        }
        else
        {
            timeSinceDigDamage += Time.deltaTime;
        }

        if (onDig != null) onDig(rockCounts);

        // Move
        lifeTime += Time.deltaTime;
        transform.position = position0 + (Vector2)transform.up * lifeTime * speed;

        // Shake
        float shakeIntensity = timeSinceDigDamage < shakeTimeWindow ? 0.015f : 0; //health < 0.25f ? 0.03f : 0.015f;
        float shakeOffset = Mathf.Sin(lifeTime * 60f) * shakeIntensity;
        shakingParent.position += transform.right * shakeOffset;

        // Damage color
        spriteR.color = Color.Lerp(Color.red, Color.white, health);

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
        Drill drill = ClientScene.FindLocalObject(drillNetId).GetComponent<Drill>();
        if (drill != null)
            Kill(drill.Owner);
    }
    [ClientRpc]
    private void RpcOnExplode()
    {
        diggerCircle.gameObject.SetActive(true);
        StartCoroutine(CoroutineUtil.DoAtEndOfFrame(() =>
        {
            exploding = true;
            health = 0;
        }));
    }


    private bool IsOutOfBounds()
    {
        return Mathf.Abs(transform.position.y) > Ground.Height * 0.7f;
    }
}
