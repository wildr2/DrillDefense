using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Building : Unit
{
    public AudioSource buildSound;
    public Transform templatePrefab;
    public abstract int Cost { get; }


    public override void Init(Player owner)
    {
        base.Init(owner);
        if (owner.IsLocalHuman())
            buildSound.PlayDelayed(0.05f);
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
