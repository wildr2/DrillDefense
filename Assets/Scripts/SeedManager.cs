using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class SeedManager : NetworkBehaviour
{
    [SyncVar] public int seed;
    [SyncVar] [NonSerialized] public bool seedSet = false;
    public Action<int> onSeedSet;

    public bool useCustomSeed = true;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (!isServer) return;

        seed = useCustomSeed ? seed : (int)DateTime.Now.Ticks;
        seedSet = true;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(SendEventOnSeedSet());
    }
    private IEnumerator SendEventOnSeedSet()
    {
        while (!seedSet) yield return null;
        if (onSeedSet != null) onSeedSet(seed);
    }
}
