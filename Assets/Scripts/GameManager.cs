using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    private Ground ground;
    public SeedManager seeder;

    public Player[] Players { get; private set; }
    public Player TopPlayer { get { return Players[0]; } }
    public Player BotPlayer { get { return Players[1]; } }
    public Player LocalHuman { get; private set; }

    public bool IsPlaying { get; private set; }
    public bool PlayersReady { get; private set; }
    private System.Action onPlayersReady;


    public void RegisterPlayer(Player player)
    {
        Players[player.id] = player;

        if (player.isLocalPlayer && !player.ai)
        {
            // local human player - flip camera if bot player
            if (!player.IsTop)
                Camera.main.transform.Rotate(Vector3.forward, 180);
        }

        Tools.Log("Registered Player " + player.id + (player.ai ? " (AI)" : ""), Color.blue);

        if (TopPlayer != null && BotPlayer != null)
        {
            OnAllPlayersRegistered();
        }
    }
    public void DoOncePlayersReady(System.Action action)
    {
        if (PlayersReady) action();
        onPlayersReady += action;
    }

    private void Awake()
    {
        ground = FindObjectOfType<Ground>();

        Players = new Player[2];
        IsPlaying = false;

        // Gererate ground after synched random seed is known
        seeder.onSeedSet += (int seed) => ground.Init(seed);
    }
    private void OnAllPlayersRegistered()
    {
        // Setup client perspective (player vision)
        if (TopPlayer.ai && BotPlayer.ai)
        {
            ground.SetVisionPOV(TopPlayer);
            ground.SetVisionPOV(BotPlayer);
        }
        else
        {
            LocalHuman = TopPlayer.IsLocalHuman() ? TopPlayer
            : BotPlayer.IsLocalHuman() ? BotPlayer : null;

            if (LocalHuman != null) ground.SetVisionPOV(LocalHuman);
        }

        // Set Ready
        PlayersReady = true;
        IsPlaying = true;

        // Event
        if (onPlayersReady != null)
            onPlayersReady();
    }
}
